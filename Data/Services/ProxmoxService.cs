using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;
using WebApplication1.Data.ProxmoxDTO.Node;
using System.Text.Json.Nodes;
using System.Reflection;


namespace WebApplication1.Data.Services;

/// <summary>
/// Обслуживание связи с Proxmox
/// </summary>
public class ProxmoxService : IProxmoxService
{
    private readonly string MASTER = "master"; // Общее название управляющих узлов
    private readonly string WORKER = "worker"; // общее название рабочих узлов
    private object _sync = new object();
    private readonly ILogger<ProxmoxService> _logger;

    private readonly IDBService _dbWorker;

    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration">type of IConfiguration</param>
    /// <param name="provision">type of IDBService</param>
    public ProxmoxService(ILogger<ProxmoxService> logger, IConfiguration configuration, IDBService provision = null)
    {
        _logger = logger;
        _dbWorker = provision;
        _configuration = configuration;
    }

    /// <summary>
    /// Запуск машин 
    /// </summary>
    /// <param name="vmIds">Список ID машин для запуска</param>
    /// <param name="proxmoxId">Идентификатор подключения Proxmox в БД</param>
    /// <returns>Словарь с id и результатом запуска</returns>
    public async Task<Dictionary<int, bool>> StartVmsAsync(List<int> vmIds, int proxmoxId)
    {
        if (!vmIds.Any()) return new Dictionary<int, bool>(); // проверка списка машин

        var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>; // получение параметров подключения
        var currentProxmox = proxmoxes?.FirstOrDefault(x => x.Id == proxmoxId); // выбор нужного подключения

        if (currentProxmox == null) return new Dictionary<int, bool>(); // проверка наличия указанного подключения

        var nodeList = (await GetProxmoxNodesListAsync(proxmoxId))
                        .Select(x => x.Node)
                        .ToList(); // получение списка узлов к кластере Proxmox

        var vmNodeMap = new Dictionary<int, string>();

        // Загружаем все ВМ для всех узлов одним вызовом в параллели
        var vmTasks = nodeList.Select(async node => new
        {
            Node = node,
            Vms = await GetAllVMsIdAndName(currentProxmox.ProxmoxURL, node, currentProxmox.ProxmoxToken)
        }).ToList();

        var vmResults = await Task.WhenAll(vmTasks);

        foreach (var result in vmResults)
        {
            foreach (var vmId in vmIds)
            {
                if (result.Vms.ContainsKey(vmId))
                {
                    vmNodeMap[vmId] = result.Node;
                }
            }
        }

        var startTasks = vmNodeMap.Select(kv => StartVm(currentProxmox, kv.Key, kv.Value)); // запуск всех машин в параллели
        var results = await Task.WhenAll(startTasks);

        return vmNodeMap.Keys.Zip(results, (id, status) => new { id, status })
                             .ToDictionary(x => x.id, x => x.status);
    }

    /// <summary>
    /// Запуск развёртывания машин в Proxmox
    /// </summary>
    /// <param name="vmInfo">Информация для развёртывания</param>
    /// <returns>Объект с результатом</returns>
    public async Task<object> StartProvisioningVMsAsync(CreateVMsDTO vmInfo)
    {
        List<object> results = new();

        if (!CheckAllParams(vmInfo)) // проверка правильности и наличия всех параметров для развёртывания
        {
            return results;            
        }

        if (vmInfo.ProvisionSchema == null) // если нет данных о размещении машин
        {
            var etcdCreationStatus = await CreateVmOfType(vmInfo, ClusterElemrntType.ETCDAndControlPlane) as List<object>; // создане управляющих узлов
            var workerCreationStatus = await CreateVmOfType(vmInfo, ClusterElemrntType.Worker) as List<object>; // создание рабочих узлов

            // добавление в список результата
            results.AddRange(ConvertToListString(etcdCreationStatus)); 
            results.AddRange(ConvertToListString(workerCreationStatus));

            return results;
        }        

        var VmsCreationStatus = await CreateVmOfType(vmInfo) as List<object>; // развёртывание машин согласно схеме размещения

        results = ConvertToListString(VmsCreationStatus); // добавление в список результата

        return results;
    }

    /// <summary>
    /// Полученеи списка хостов к клостере Proxmox
    /// </summary>
    /// <param name="proxmoxId"> Id подключения из БД</param>
    /// <returns>Список доступных хостов</returns>
    public async Task<List<ProxmoxNodeInfoDTO>> GetProxmoxNodesListAsync(int proxmoxId)
    {
        var nodesList = new List<ProxmoxNodeInfoDTO>();

        var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>; // получене данных подключения
        var proxmoxConn = proxmoxes?.Where(x => x.Id == proxmoxId).FirstOrDefault();
        if (proxmoxConn == null) // проверка валидности запрошенного подключения
        {
            return nodesList;
        }

        var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes", HttpMethod.Get, proxmoxConn.ProxmoxToken); // отпрпвка запрса в Proxmox

        //  разбор данных и формирование списка хостов
        if (response != null && response.Data is JArray jArray)
        {
            var nodesInfoList = jArray.ToObject<List<ProxmoxNodeInfoDTO>>();

            if (nodesInfoList != null)
            {
                nodesList.AddRange(nodesInfoList);
            }
        }

        return nodesList;
    }    

    /// <summary>
    ///  Получение вписка всех шаблонов машин в кластере Proxmox
    /// </summary>
    /// <param name="nodesName">Список хостов для поиска</param>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <returns>Словарь с Id шаблона и его именем</returns>
    public async Task<Dictionary<int, string>> GetAllNodesTemplatesIds(List<string> nodesName, int proxmoxId)
    {
        if (nodesName == null || !nodesName.Any()) return new Dictionary<int, string>(); // проверка валидности входных данных

        var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>; // запрсс подключения из БД
        var proxmoxConn = proxmoxes?.FirstOrDefault(x => x.Id == proxmoxId);

        if (proxmoxConn == null) return new Dictionary<int, string>(); // провекак валидности запрошенного подключения

        var tasks = nodesName.Select(async node =>
        {
            var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken); // отправка запроса в Proxnox

            if (response?.Data is not JArray jArray) return new List<(int, string)>();

            return jArray.ToObject<List<ProxmoxQemuDTO>>()?
                .Where(q => q.Template)
                .Select(q => (q.VmId, q.Name))
                .ToList() ?? new List<(int, string)>(); // формирование списка шаблонов для одного хоста
        });

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(x => x)
                      .GroupBy(q => q.Item2)
                      .Select(g => g.First()) // Убираем дубликаты
                      .ToDictionary(q => q.Item1, q => q.Item2);  // общий список шаблонов без дубликатов для всех хостов
    }

    /// <summary>
    /// Ожидание готовности машин к подключению к Rancher
    /// </summary>
    /// <param name="vmsRunningState">Словарь с состоянием машин</param>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <param name="connectionString">Строка подключения к Rancher</param>
    /// <returns>Словарь с результом подключения к Rancher</returns>
    public async Task<Dictionary<int, bool>> WaitReadyStatusAsync(Dictionary<int, bool> vmsRunningState, int proxmoxId, string connectionString)
    {
        if (vmsRunningState == null || vmsRunningState.Count == 0) return new Dictionary<int, bool>(); // порверка валидности машин

        var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>;
        var currentProxmox = proxmoxes?.FirstOrDefault(x => x.Id == proxmoxId);
        if (currentProxmox == null) return new Dictionary<int, bool>(); // проверка валидновти подключения

        var tasks = vmsRunningState.Select(async vm => new
        {
            VmId = vm.Key,
            IsReady = await GetReadyStateOfVM(vm.Key, currentProxmox, connectionString) // запуск порверки готовности
        });

        var results = await Task.WhenAll(tasks); 
        return results.ToDictionary(x => x.VmId, x => x.IsReady);
    }

    /// <summary>
    /// Получение списка шаблонов для всего кластера Proxmox
    /// </summary>
    /// <param name="data">Id подключения к Proxmox из БД</param>
    /// <returns>JSON со списком шаблонов</returns>
    public async Task<JsonResult> GetTemplate([FromBody] ProxmoxIdDTO data)
    {
        try
        {
            var options = new List<SelectOptionDTO>();

            var nodesNameList = new List<string>();

            var proxmoxId = data.ProxmoxId;

            var nodesList = await GetProxmoxNodesListAsync(proxmoxId); // получение списка хостов в кластере
            nodesNameList.AddRange(nodesList.Select(x => x.Node));
            //var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
            if (nodesList.Count > 0)
            {
                var templates = await GetAllNodesTemplatesIds(nodesNameList, proxmoxId); // получение шаблонов

                foreach (var template in templates)
                {
                    options.Add(new SelectOptionDTO { Value = template.Key.ToString(), Text = template.Value });
                }
                return new JsonResult(options);
            }
            else
            {
                return new JsonResult(options);
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new List<SelectOptionDTO>());
        }
    }

    /// <summary>
    /// Получение параметров шаблона
    /// </summary>
    /// <param name="data">ID шаблона(VM ID)</param>
    /// <returns>JOSN со списком параметров выбранного шаблона</returns>
    /// <exception cref="ArgumentException">Не правильный ID шаблона</exception>
    public async Task<JsonResult> GetTemplateParams([FromBody] TemplateIdDTO data)
    {
        try
        {
            var proxmoxId = data.ProxmoxId;
            var templateId = data.TemplateId;
            var templateParams = await GetVmInfoAsync(proxmoxId, templateId); // получение данных шаблона

            return new JsonResult(new TemplateParams { 
                CPU = templateParams.CPUS.ToString(),
                RAM = $"{FormatBytes(templateParams.MaxMem):0.0}", // конвертирование в удобный для чтения формат
                HDD = $"{FormatBytes(templateParams.MaxDisk):0.0}" // конвертирование в удобный для чтения формат
            });
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"check {nameof(GetTemplateParams)} in {nameof(ProxmoxService)}");
        }
    }

    /// <summary>
    /// Создание нового подключения Proxmox в БД
    /// </summary>
    /// <param name="model">Параметры подключения</param>
    /// <returns>JSON с результатом создания</returns>
    public async Task<JsonResult> CreateNewProxmoxCred([FromBody] ProxmoxModel model)
    {

        if (string.IsNullOrEmpty(model.ProxmoxURL)) // пустой URL
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Proxmox url is empty" });
        }

        if (string.IsNullOrEmpty(model.ProxmoxToken)) // пустой токен
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Proxmox Token is empty" });
        }

        if (!await _dbWorker.CheckDBConnection()) // ПРоверка подключения к БД
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Database is unreachable" });
        }

        if (model.DefaultConfig == null) // не задан конфиг по умолчанию
        {
            model.DefaultConfig = new ProxmoxDefaultConfig();
        }

        if (await _dbWorker.AddNewCred(model)) // добавление новой записи
        {
            
            return new JsonResult(new Response { Status = Status.OK, Message = "New Proxmox Creds was successfully added" });
        }

        return new JsonResult(new Response
        {
            Status = Status.WARNING,
            Message = "New Proxmox Creds wasn`t added.\n" +
            "Maybe you try to add existing data.\n Contact an adnimistrator"
        });
    }

    /// <summary>
    /// Checks an ability to create all vms according to oversubscription
    /// </summary>
    /// <param name="param">Info for creating VMs</param>
    /// <returns>Boolean True = Available, False = Not</returns>
    public async Task<Dictionary<string, List<string>>?> CheckCreationAbility(CreateVMsDTO param)
    {
        try
        {
            // выставленрие ограничений развёртывания
            var cpuLimitConfig = _configuration["CPUUsageLimit"] ?? "0.8"; // предел текущего использования CPU
            var oversubConfig = _configuration["OverSubscriptionLimit"] ?? "2.5"; // предел переподписки
            var CPU_USAGE_LIMIT = double.Parse(cpuLimitConfig, CultureInfo.InvariantCulture);
            var OVERSUBSCRIPTION_LIMIT = double.Parse(oversubConfig, CultureInfo.InvariantCulture);
            var VMsAllocation = new Dictionary<string, List<string>>();

            if (param == null) // пыстые входные данные
            {
                return null;
            }

            if (param.EtcdAndControlPlaneAmount > param.ETCDProvisionRange.Count) // колличество управляющих узлов больше чет коллитчество хостов выделенных под управляющте узлы
            {
                return null;
            }

            var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>;
            var currentProxmox = proxmoxes?.Where(x => x.Id == param.ProxmoxId).FirstOrDefault();

            if (currentProxmox == null) // проверка валидности выбранного подключения
            {
                return null;
            }

            var allNodesInfo = await GetProxmoxNodesListAsync(param.ProxmoxId); // получение списка всех хостов в кластере
            var nodeList = allNodesInfo.Select(x => x.Node).ToList();

            if (!nodeList.Any()) // валидность наличия хостов
            {
                return null;
            }

            // проверка перепподписки для всех узлов
            var currentOversub = new Dictionary<string, NodeOversubscriptionDTO>();
            foreach (var node in nodeList)
            {
                var nodeOversub = await CountOversubscription(currentProxmox, node, CPU_USAGE_LIMIT); // подсчёт переподписки
                currentOversub.Add(node, nodeOversub);
            }

            CheckMinimalRequarements(param); // setting min vm params if values too low, depending on config

            // проверка будущей переподписки после добавления узла для каждой машины 
            for (int i = 0; i < param.EtcdAndControlPlaneAmount; i++)
            {
                foreach (var node in currentOversub.OrderBy(x => x.Value.CurrentOversubscription))
                {
                    if (!VMsAllocation.ContainsKey(node.Key) && currentOversub[node.Key].CurrentOversubscription < OVERSUBSCRIPTION_LIMIT && param.ETCDProvisionRange.Contains(node.Key))
                    {
                        VMsAllocation.Add(node.Key, new List<string> { MASTER + $"-0{i + 1}" });
                        currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.etcdConfig.CPU);
                        break;
                    }
                    else if(VMsAllocation.ContainsKey(node.Key) || !param.ETCDProvisionRange.Contains(node.Key)) // если управляющая машина уже добавлена, то выбирается другой хост для добавления
                    {
                        continue;
                    }

                        return null;
                }
            }            

            // распределение рабочих узлов на основе сохранения наименьшей переподписки по всем хостам PRoxmox Кластера
            for (int i = 0; i < param.WorkerAmount; i++)
            {
                foreach (var node in currentOversub.OrderBy(x => x.Value.CurrentOversubscription))
                {
                    if (currentOversub[node.Key].CurrentOversubscription < OVERSUBSCRIPTION_LIMIT && param.WorkerProvisionRange.Contains(node.Key))
                    {
                        if (VMsAllocation.ContainsKey(node.Key))
                        {
                            VMsAllocation[node.Key].Add(WORKER + $"-0{i + 1}");
                            currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                            break;
                        }
                        else
                        {
                            VMsAllocation.Add(node.Key, new List<string> { WORKER + $"-0{i + 1}" });
                            currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                            break;
                        }
                    }
                }
            }

            return VMsAllocation;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Some error in {nameof(ProxmoxService.CheckCreationAbility)}\n" +
                $"{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns list of avalable storages in proxmox cluster
    /// </summary>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <returns>Список доступных хранилищ</returns>
    /// <exception cref="Exception">Если возникает любая ошибка</exception>
    public async Task<List<ProxmoxResourcesDTO>> GetProxmoxStoragesAsync(int proxmoxId)
    {
        try
        {
            var resources = await GetProxmoxResources(proxmoxId); // полкчение все ресурсов кластера

            return resources.Where(x => x.Type == "storage").ToList(); // возврат только хранилищ
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + "\n" + ex.InnerException?.Message);
            return new List<ProxmoxResourcesDTO>();
        }
    }

    /// <summary>
    /// Crerates a List of all Proxmox Cluster/Host resources
    /// </summary>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <returns>Returns Liat Of ProxmoxResourcesDTO</returns>
    public async Task<List<ProxmoxResourcesDTO>> GetProxmoxResources(int proxmoxId)
    {
        try
        {
            var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>;
            var currentProxmox = proxmoxes?.Where(x => x.Id == proxmoxId).FirstOrDefault();

            if (currentProxmox == null) // проверка валидности подключения 
            {
                return null;
            }

            var url = $"{currentProxmox.ProxmoxURL}/api2/json/cluster/resources";
            var clusterInfo = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken); // отправка запроса

            if (clusterInfo == null || clusterInfo.Data == null) // данные не получены
            {
                return new List<ProxmoxResourcesDTO>();
            }

            var resources = JsonConvert.DeserializeObject<List<ProxmoxResourcesDTO>>(clusterInfo.Data.ToString());



            return resources.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + "\n" + ex.InnerException?.Message);
            return new List<ProxmoxResourcesDTO>();
        }
    }

    /// <summary>
    /// Получение ресурсов кластера/хоста PRoxmox по уникальному имени
    /// </summary>
    /// <param name="uniqueProxmoxName">Уникальное имя</param>
    /// <returns>Returns Liat Of ProxmoxResourcesDTO</returns>
    public async Task<List<ProxmoxResourcesDTO>> GetProxmoxResources(string uniqueProxmoxName)
    {
        var proxmoxId =await GetProxmoxID(uniqueProxmoxName); // получение id на основе уникального имени

        if (proxmoxId < 0)
        {
            return new List<ProxmoxResourcesDTO>();
        }

        return await GetProxmoxResources(proxmoxId);
    }

    /// <summary>
    /// Gets list of available VLAN tags in Proxmox Cluster/host
    /// </summary>
    /// <param name="proxmoxId">Id of Proxmox connection from DB</param>
    /// <returns>Returns list of string representing IDs of Proxomox VLAN Tags</returns>
    public async Task<List<string>> GetProxmoxVLANTags(int proxmoxId)
    {
        var proxmoxes = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox)) as List<ProxmoxModel>; // получение всех подключений
        var currentProxmox = proxmoxes?.Where(x => x.Id == proxmoxId).FirstOrDefault(); // выбор нужного подключения

        var node = (await GetProxmoxNodesListAsync(proxmoxId)).Select(x => x.Node).FirstOrDefault(); // выбор любого хоста в кластере

        var netList = new List<ProxmoxNetworkInfoDTO>();
        var url = $"{currentProxmox!.ProxmoxURL}/api2/json/nodes/{node}/network";
        var responce = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken); // отправка запроса в Proxmox 

        if (responce.Data == null) // проверка усторы ответа
        {
            return new List<string>();
        }

        //_logger.LogInformation(responce.Data.ToString());
        var netInfo = JsonConvert.DeserializeObject<List<ProxmoxNetworkInfoDTO>>(responce!.Data!.ToString()!); // разбор ответа

        netList.AddRange(netInfo!); // добавление всех Vlan

        var result = netList.Where(x => x.VlanId != null).Select(x => new string(x.VlanId)).Distinct().ToList(); // извлеченение уникальных VlanId  в список
        return  result ?? new List<string>();
    }

    /// <summary>
    /// Получение всей информации о манине
    /// </summary>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <param name="vmId">Id of vm in Proxmox</param>
    /// <returns>Объект с информацией о вм</returns>
    public async Task<VmInfoDTO> GetVmInfoAsync(int proxmoxId, int vmId)
    {
        var nodesList = await GetProxmoxNodesListAsync(proxmoxId); // получене всего списка хостов в кластере

        var proxmoxes = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>;
        var proxmoxConn = proxmoxes?.Where(x => x.Id == proxmoxId).FirstOrDefault();

        if (proxmoxConn == null) // проверка валидности подключения 
        {
            return null;
        }

        // проверка наличия машины на каждом хосте в кластере 
        foreach (var node in nodesList.Select(x => x.Node))
        {
            var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken);


            if (response != null && response.Data is JArray jArray)
            {
                var qemuList = jArray.ToObject<List<VmInfoDTO>>();

                var info = qemuList.FirstOrDefault(x => x.VmId == vmId);

                if (info == null)
                {
                    continue;
                }

                return info;
            }
        }

        return new VmInfoDTO();
    }

    /// <summary>
    /// Получение всей информации о манине, по уникальному имени Proxmox
    /// </summary>
    /// <param name="uniqueProxmoxName">Уникальное имя Proxmox</param>
    /// <param name="vmId">Id of vm in Proxmox</param>
    /// <returns>Объект с информацией о вм</returns>
    public async Task<VmInfoDTO> GetVmInfoAsync(string uniqueProxmoxName, int vmId)
    {
        var proxmoxId = await GetProxmoxID(uniqueProxmoxName); // получение id  по имени

        if (proxmoxId < 0)
        {
            return new VmInfoDTO();
        }

        return await GetVmInfoAsync(proxmoxId, vmId);
    }

    /// <summary>
    /// Получение всего списка подключений Proxmox
    /// </summary>
    /// <returns>Список подключений</returns>
    /// <exception cref="NullReferenceException">Отсуствует подключение к БД</exception>
    public async Task<List<ProxmoxModel>> GetAllProxmox()
    {
        if (!await _dbWorker.CheckDBConnection())
        {
            throw new NullReferenceException();
        }

        var allProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox)) as List<ProxmoxModel>;

        return allProxmox ?? new List<ProxmoxModel>();
    }

    /// <summary>
    /// Обновление информации о подключении
    /// </summary>
    /// <param name="reconfig">Данные для замены</param>
    /// <returns>Bool result of operation</returns>
    /// <exception cref="NullReferenceException">Отсуствует подключение к БД</exception>
    public async Task<bool> UpdateProxmox([FromBody] ProxmoxDefaultReconfig reconfig)
    {
        if (!await _dbWorker.CheckDBConnection())
        {
            throw new NullReferenceException();
        }

        var currentProxmox = ((await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox)) as List<ProxmoxModel>).Where(x => x.Id == reconfig.Id).FirstOrDefault();

        if (currentProxmox == null) // проверка валидности выбранного подключения
        {
            return false;
        }

        // замена параметров на новые
        currentProxmox.DefaultConfig = reconfig.DefaultConfig; 
        currentProxmox.ProxmoxUniqueName = reconfig.UniqueProxmoxName;

        if (await _dbWorker.Update(currentProxmox)) // обновление данных в БД
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получение параметров подключения по имени
    /// </summary>
    /// <param name="uniqueProxmoxName">Уникальное имя</param>
    /// <returns>Id of connection from DB</returns>
    public async Task<int> GetProxmoxCred(string uniqueProxmoxName)
    {
        if (!await _dbWorker.CheckDBConnection()) // порверка доступности БД
        {
            return -1;
        }

        var rancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.ProxmoxUniqueName == uniqueProxmoxName).FirstOrDefault();
        if (rancher != null)
        {
            return rancher.Id;
        }

        return -1;
    }

    /// <summary>
    /// Выставление паметров по умолчанию для развертывания машин
    /// </summary>
    /// <param name="createVMsDTO">Объект для развёртывания со спецификацией мешин</param>
    /// <param name="environment">Окружение развёртывания</param>
    /// <returns>ОБновлённый объект лоя развёртывания</returns>
    public async Task<CreateVMsDTO?> SetProxmoxDefaultValues(CreateVMsDTO createVMsDTO, string environment)
    {
        var proxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == createVMsDTO.ProxmoxId).FirstOrDefault();

        if (proxmox == null) // валидация подключения
        {
            return null;
        }

        createVMsDTO.ETCDProvisionRange = proxmox.DefaultConfig.EtcdProvisionRange; // выставления диапазона развёртывания управляющих узлов
        createVMsDTO.WorkerProvisionRange = proxmox.DefaultConfig.WorkerProvisionRange; // выставления диапазона развёртывания рабочих узлов
        createVMsDTO.EtcdAndControlPlaneAmount = proxmox.DefaultConfig.ControlPlaneAmount; // выставления колличества управляющих узлов
        createVMsDTO.SelectedStorage = [proxmox.DefaultConfig.SelectedStorage]; //выставления диапазона развёртывания по хранилищам

        // определение окружения для развертывания
        environment = environment.ToUpperInvariant();

        if (string.Equals(environment, "PROD"))
        {
            createVMsDTO.SelectedVlan = proxmox.DefaultConfig.VlanProd; // выбор prod VlanTag
        }
        else 
        {
            createVMsDTO.SelectedVlan = proxmox.DefaultConfig.VlanTest; // выбор test VlanTag
        }

        createVMsDTO.VMTemplateName = proxmox.DefaultConfig.VmTemplateName; // выставление имени шаблона развёртывания
        createVMsDTO.etcdConfig = proxmox.DefaultConfig.EtcdConfig; // выставление параметров по умолчанию для управляющих узлов

        var maxVMIdInProxmox = (await GetProxmoxResources(proxmoxId: proxmox.Id)).Max(x => x.VmId);
        createVMsDTO.VMStartIndex = maxVMIdInProxmox + 1; // выставление номера машины на 1 больше чем максимальное значение в текущий момент


        return createVMsDTO;
    }

    /// <summary>
    /// Подсчёт переподписки
    /// </summary>
    /// <param name="currentProx">Подключение к кластеру</param>
    /// <param name="nodeName">имя хотся</param>
    /// <param name="cpuLimit">ограничение использования CPU</param>
    /// <returns>Объект с текущей переподпиской хоста</returns>
    private async Task<NodeOversubscriptionDTO> CountOversubscription(ProxmoxModel currentProx, string nodeName, double cpuLimit)
    {
        var url = $"{currentProx.ProxmoxURL}/api2/json/nodes/{nodeName}/status";
        var clusterStatus = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProx.ProxmoxToken);

        if (clusterStatus == null) // валидация подключения
        {
            return new NodeOversubscriptionDTO { TotalAllocatedCPUs = 6, TotalNodeCPU = 2 }; // данные которые превышают разрешённую переподписку
        }

        var nodeData = JsonConvert.DeserializeObject<ProxmoxNodeStatusDTO>(clusterStatus?.Data?.ToString()!);

        if (nodeData?.CPU > cpuLimit) // проверка предела испольрования процессора
        {
            return new NodeOversubscriptionDTO { TotalAllocatedCPUs = 6, TotalNodeCPU = 2 };
        }

        var totalCpus = 0;
        var allVms = await GetAllNodeVMsInfo(currentProx.ProxmoxURL, nodeName, currentProx.ProxmoxToken); // получене всех машин

        // сумма всех выделенных ядер, кроме шаблонов
        foreach (var vm in allVms)
        {
            if (!vm.Template)
            {
                totalCpus += vm.CPUs;
            }                
        }

        return new NodeOversubscriptionDTO { TotalNodeCPU = nodeData!.CPUInfo.CPUs, TotalAllocatedCPUs = totalCpus };        
    }

    /// <summary>
    /// Получение Id подключения по имени Proxmox
    /// </summary>
    /// <param name="uniqueName">Уникальное имя</param>
    /// <returns>Id подключения к Proxmox из БД</returns>
    private async Task<int> GetProxmoxID(string uniqueName)
    {
        if (string.IsNullOrEmpty(uniqueName)) // пустое имя
        {
            return -1;
        }
        var proxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).FirstOrDefault(x => x.ProxmoxUniqueName == uniqueName); // подключение по уникальному имени

        if (proxmox == null)
        {
            return -1;
        }

        return proxmox.Id;
    }

    /// <summary>
    /// Отправка запрса на Proxmox
    /// </summary>
    /// <param name="url">Адрес подключения</param>
    /// <param name="httpMethod">Тип запорса</param>
    /// <param name="token">Токен подключения</param>
    /// <param name="data">Полезная нагрузка</param>
    /// <returns>Ответ от Proxmox</returns>
    private async Task<ProxmoxResponse> SendRequestToProxmoxAsync(string url, HttpMethod httpMethod, string token, object? data = null)
    {
        try
        {
            //Ignore certificate checking
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(handler);
            HttpRequestMessage request = new();
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                if (data != null)
                {
                    // Create the content with the JSON payload
                    var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

                    request = new HttpRequestMessage(httpMethod, url)
                    {
                        Content = content,
                    };
                }
                else
                {
                    request = new HttpRequestMessage(httpMethod, url);
                }
            }
            else if (httpMethod == HttpMethod.Get)
            {
                request = new HttpRequestMessage(httpMethod, url);
            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", token);

            var response = await httpClient.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError && response.ReasonPhrase.Contains("pid"))
            {
                _logger.LogError(request.RequestUri + "\n" + response.ReasonPhrase);
                //throw new Exception(await response.Content.ReadAsStringAsync());
            }

            var result = await response.Content.ReadAsStringAsync();

            //_logger.LogInformation($"{url}\n{result}");
            
            return JsonConvert.DeserializeObject<ProxmoxResponse>(result ?? /*lang=json,strict*/ "{ \"data\": \"null\" }");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(ProxmoxService)} \n" + ex.Message + $"\n{ex?.InnerException}");
            return new ProxmoxResponse { Error = $"Error in {nameof(SendRequestToProxmoxAsync)}", Data = null };
        }
    }

    /// <summary>
    /// Проверка всех параметров при развёртывании
    /// </summary>
    /// <param name="vmInfo">Информация о рзвёртывании машины</param>
    /// <returns>Bool value of passing checks</returns>
    private bool CheckAllParams(CreateVMsDTO vmInfo)
    {
        if (vmInfo?.EtcdAndControlPlaneAmount > 0 && vmInfo?.ProxmoxId > 0 && vmInfo?.VMTemplateName.Length > 0 &&
            vmInfo?.WorkerAmount > 0 && vmInfo?.RancherId > 0 && vmInfo.ClusterName != null &&
            _dbWorker != null && vmInfo?.VMStartIndex > 0)
        {
            if (vmInfo.VMPrefix == string.Empty || vmInfo.VMPrefix == null)
            {
                vmInfo.VMPrefix = "rke2-";
            }

            CheckMinimalRequarements(vmInfo); // проверка параметров по умолчанию
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create VM of specified type. Must be used after CheckAllParamsAsync()
    /// </summary>
    /// <param name="elemrntType">Type from ClusterElemrntType enum.</param>
    /// <param name="vmInfo">Instance of CreateVmDTO.</param>
    /// <returns>false if something wrong, List of Vms if all OK</returns>
    private async Task<object> CreateVmOfType(CreateVMsDTO vmInfo, ClusterElemrntType elemrntType = default)
    {
        if (_dbWorker == null) // проверка подключения к БД
        {
            return false;
        }

        var proxmox = await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>;
        var proxmoxCred = proxmox?.Where(x => x.Id == vmInfo.ProxmoxId).FirstOrDefault();

        if (proxmoxCred == null) // порверка подключения к PRoxmox
        {
            return false;
        }

        var nodeSource = await GetProxmoxNodesListAsync(vmInfo.ProxmoxId);
        var nodesList = nodeSource.Select(x => x.Node).ToList();

        if (nodesList == null) // проверка наличия хостов
        {
            return false;
        }

        string currentTemplateNode = string.Empty;
        int currentTemplateId = 0;

        var data = new FullCloneDTO { Full = true, Name = vmInfo.VMPrefix, VMId = currentTemplateId, NewId = vmInfo.VMStartIndex, Node = currentTemplateNode, Storage = "test" }; // шаблон для заполения данными

        var vmList = new List<object>();

        if (vmInfo.ProvisionSchema != null) // если есть схема развёртывания
        {
            var counter = 0;
            var newIdIndex = data.NewId;

            foreach (var vmProvition in vmInfo.ProvisionSchema)
            {
                var currentNodeVMs = await GetAllVMsIdAndName(proxmoxURL: proxmoxCred.ProxmoxURL, nodeName: vmProvition.Key, accessToken: proxmoxCred.ProxmoxToken);
                currentTemplateId = currentNodeVMs.Where(x => x.Value.Equals(vmInfo.VMTemplateName)).FirstOrDefault().Key; // получения ID шаблона для развёртывания
                foreach (var vmName in vmProvition.Value)
                {
                    // для каждой VM, выставление параметров
                    data.Name = vmInfo.ClusterName + '-' + vmName;
                    data.NewId = newIdIndex + counter++;
                    data.Node = vmProvition.Key;
                    data.VMId = currentTemplateId;
                    data.Storage = await SelectMaxStorageSize(vmInfo.SelectedStorage, vmInfo.ProxmoxId);
                    currentTemplateNode = vmProvition.Key;
                    var payload = JsonConvert.SerializeObject(data);


                    vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload)); // создание VM b добавление в список

                    if (data.Name.Contains(MASTER)) // если узел управления
                    {
                        await Reconfigure(proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig, vmInfo.SelectedVlan); // переконфигурация машины
                    }
                    else // если рабочий узел
                    {
                        await Reconfigure(proxmoxCred, currentTemplateNode, data.NewId, vmInfo.VMConfig, vmInfo.SelectedVlan); // переконфигурация машины
                    }
                }
            }

            return vmList;
        }
        else // если отсутствует схема размещения машин
        {
            switch (elemrntType)
            {
                case ClusterElemrntType.ETCDAndControlPlane:
                    {
                        for (var i = 0; i < vmInfo.EtcdAndControlPlaneAmount; i++)
                        {
                            data.Name += $"-{MASTER}-" + i + 1;
                            data.NewId += i;
                            var payload = JsonConvert.SerializeObject(data);

                            vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

                            await Reconfigure(proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig, vmInfo.SelectedVlan); // переконфигурация машины
                        }

                        return vmList;
                    }
                case ClusterElemrntType.Worker:
                    {
                        for (var i = 0; i < vmInfo.WorkerAmount; i++)
                        {
                            data.Name += $"-{WORKER}-" + i + 1;
                            data.NewId += vmInfo.EtcdAndControlPlaneAmount + i;
                            var payload = JsonConvert.SerializeObject(data);

                            vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

                            await Reconfigure(proxmoxCred, currentTemplateNode, data.NewId, vmInfo.VMConfig, vmInfo.SelectedVlan); // переконфигурация машины
                        }

                        return vmList;
                    }
                default:
                    return false;
            }
        }            
    }

    /// <summary>
    /// Выбор хранилища с наибольшим доступным пространством
    /// </summary>
    /// <param name="storageSource">Список хранилищ</param>
    /// <param name="proxmoxId">Id подключения к Proxmox из БД</param>
    /// <returns>Имя хранилища с наибольшим свободным пространством</returns>
    private async Task<string> SelectMaxStorageSize(List<string> storageSource, int proxmoxId)
    {
        var allStorages = await GetProxmoxStoragesAsync(proxmoxId);
        var maxStorageSizeName = allStorages.OrderBy(x => x.MaxDisk - x.Disk).Select(x => x.Storage).Intersect(storageSource).FirstOrDefault();
        return maxStorageSizeName ?? string.Empty;
    }

    /// <summary>
    /// Создание машины
    /// </summary>
    /// <param name="proxmoxURL">Адрес подключения</param>
    /// <param name="nodeName">имя хоста</param>
    /// <param name="accessToken">Токен доступа</param>
    /// <param name="vmTemplateId">ID шаблона развёртывания</param>
    /// <param name="payload">Полезная нагрузка</param>
    /// <returns>Объект с результатом развёртывания</returns>
    private async Task<object> CreateVM(string proxmoxURL, string nodeName, string accessToken, int vmTemplateId, object payload)
    {
        var newVMID = 0;

        if (payload is string source)
        {
            // Получение нового индкса разрёртывания
            var elem = "\"newid\":"; // ноавя мишина
            var startIndex = source.IndexOf(elem) + elem.Length; // начальный индекс
            var endindex = source.IndexOf(',', startIndex); 
            var number = source[startIndex..endindex];
            newVMID = int.Parse(number);
        }

        try
        {
            var nodeVMs = await GetAllVMsIdAndName(proxmoxURL, nodeName, accessToken); // получение всех машин, из имен и ID

            if (nodeVMs.Keys.Contains(newVMID))
            {
                return $"{newVMID} already exist";
            }

            var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/{vmTemplateId}/clone", HttpMethod.Post, accessToken, payload); // отправка запроса на клонирование машины

            var upid = response.Data.ToString(); // получение UID задачи для контроля состояния


            while (true)
            {
                var status = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/tasks/{upid}/status", HttpMethod.Get, accessToken); // проверка статуса задачи клонирования
                var result = status.Data.ToString();
                var taskStatus = JsonConvert.DeserializeObject<TaskStatusDTO>(result);

                if (taskStatus != null && taskStatus.Status == "stopped" && taskStatus.ExitStatus != null)
                {
                    if (taskStatus.ExitStatus == "OK")
                    {
                        return newVMID;
                    }
                    else
                    {
                        return $"Error while creating {newVMID} VM";
                    }
                }

                await Task.Delay(100);
            }
        }
        catch (ArgumentNullException ex)
        {
            return "";
        }

    }

    /// <summary>
    /// Получение словаря с именами и ID машин на хосте Proxmox
    /// </summary>
    /// <param name="proxmoxURL">Адрес подключения</param>
    /// <param name="nodeName">имя хоста</param>
    /// <param name="accessToken">Токен доступа</param>
    /// <returns>Словарь с ID  и именами</returns>
    private async Task<Dictionary<int,string>> GetAllVMsIdAndName(string proxmoxURL, string nodeName, string accessToken)
    {
        var vmList = new Dictionary<int, string>();

        var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/", HttpMethod.Get, accessToken); // отправка запроса на proxmox

        // валидация и передор отвера с выделением Id и имени
        if (response != null && response.Data is JArray jArray)
        {
            var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

            if (qemuList != null)
            {
                foreach (var qemu in qemuList)
                {
                    vmList.Add(qemu.VmId,qemu.Name);
                }
            }
        }

        return vmList;
    }

    /// <summary>
    /// получение полного списка Vm со всей информацией для proxmox хоста
    /// </summary>
    /// <param name="proxmoxURL">Адрес плдключения</param>
    /// <param name="nodeName">Имя хоста</param>
    /// <param name="accessToken">Токен доступа</param>
    /// <returns>Список машин на хосте</returns>
    private async Task<List<ProxmoxQemuDTO>> GetAllNodeVMsInfo(string proxmoxURL, string nodeName, string accessToken)
    {
        var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/", HttpMethod.Get, accessToken);

        if (response != null && response.Data is JArray jArray)
        {
            var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

            if (qemuList != null)
            {
                return qemuList;
            }
        }

        return new List<ProxmoxQemuDTO>();
    }

    /// <summary>
    /// Запуск минин
    /// </summary>
    /// <param name="currentProxmox">Плдключение к Proxmox</param>
    /// <param name="vmId">ID машины</param>
    /// <param name="nodeName">имя хоста</param>
    /// <returns></returns>
    private async Task<bool> StartVm(ProxmoxModel currentProxmox, int vmId, string nodeName)
    {
        var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/current"; // запрос текущего статуса

        var response = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);


        var data = JsonConvert.DeserializeObject<QemuStatusDTO>(response.Data.ToString());

        while (true)
        {
            url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/start"; // запуск машины

            response = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken);
            if (response.Data != null)
            {
                url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/current"; // запрос текущего статуса

                response = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);
                data = JsonConvert.DeserializeObject<QemuStatusDTO>(response.Data.ToString());

                if (data.Status == "running")
                {
                    break;
                }
            }
            await Task.Delay(3000);
        }

        return true;
    }

    /// <summary>
    /// Получение готового состояния машины к подключению к Rancher
    /// </summary>
    /// <param name="vmId">Id VM</param>
    /// <param name="currentProxmox">подключения к Proxmox из БД</param>
    /// <param name="connectionString">Строка подключения</param>
    /// <returns>Bool value of ready state</returns>
    private async Task<bool> GetReadyStateOfVM(int vmId, ProxmoxModel currentProxmox, string connectionString)
    {

        await Task.Delay(10 * 1_000);
        var linuxCommand = "journalctl | grep 'cloud-init' | grep 'finished' | grep 'Up'"; // команда для исполнеия внутри машины
        var IsVmReady = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand); // отправка крманды
        
        while(IsVmReady == null) // если нет данных
        {
            IsVmReady = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand); // отправка оманды
        }


        if (!(bool)IsVmReady) // если машина не готова или возникли ошибки
        {
            return false;
        }

        // список команд для выполнения
        var listOfCommands = new List<string> 
        {
            "sudo apt update -y",
            "sudo apt upgrade -y",
            //"sudo dpkg --configure -a",
            "sudo apt install linux-generic nfs-common net-tools -y",
            "sudo reboot",
            "ip a"
            
        };

        if (!await SendGroupOfCommands(vmId, currentProxmox, listOfCommands)) // отправка списка команд
        {           
            return false;
        }

        var vmName = (await GetVmInfoAsync(currentProxmox.Id, vmId)).Name; // поучение имени машины
        bool vmReady = false;
        if (vmName.Contains(MASTER) ) // если узел управления
        {
            linuxCommand = SetLinuxConnectCommand(connectionString, " --etcd --controlplane"); // создание команды 
            var readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand);  // отправки команды
            while (readyStatus == null) // пока не готова
            {
                readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand); // отправки команды
            }
            vmReady = (bool)readyStatus;
        }
        else if (vmName.Contains(WORKER)) // если рабочий узел
        {
            linuxCommand = SetLinuxConnectCommand(connectionString, $" --{WORKER}"); // создание команды 
            var readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand); // отправки команды
            while (readyStatus == null) // пока не готова
            {
                readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, linuxCommand); // отправки команды
            }
            vmReady = (bool)readyStatus;
        }

        return vmReady;
    }

    /// <summary>
    /// Конвертирование в удобный для использования формат
    /// </summary>
    /// <param name="bytes">число в байтах</param>
    /// <returns>Короткое представление числа</returns>
    private double FormatBytes(long bytes)
    {
        const long OneKB = 1024;
        const long OneMB = OneKB * 1024;
        const long OneGB = OneMB * 1024;

        if (bytes >= OneGB)
        {
            return bytes / (double)OneGB;
        }
        else if (bytes >= OneMB)
        {
            return bytes / (double)OneMB;
        }
        else if (bytes >= OneKB)
        {
            return bytes / (double)OneKB;
        }
        else
        {
            return bytes;
        }
    }

    /// <summary>
    /// Отправка команды в машину
    /// </summary>
    /// <param name="vmId">ID of vm</param>
    /// <param name="currentProxmox">подключения к Proxmox из БД</param>
    /// <param name="linuxCommand">Команда для исполнения</param>
    /// <returns>Nullable Bool with result</returns>
    private async Task<bool?> SendQemuGuestCommand(int vmId, ProxmoxModel currentProxmox,string linuxCommand)
    {        
        try
        {
            string node = string.Empty;
            node = await GetNodeName(currentProxmox, vmId); // подучение имени хочта, где развёрнума машина
            
            if (string.IsNullOrEmpty(node)) // если пустое
            {
                return null;
            }

            var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec"; // формирование URL
            var command = new QemuGuestCommandDTO // представление команды для исполнения
            {
                Command = "/bin/bash",
                InputData = linuxCommand,
            };

            var payload = JsonConvert.SerializeObject(command); // обёртка в назрузку

            var responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload); // отправка команды

            if (command.InputData.Contains("reboot")) // если перезагрузка
            {
                return true;
            }

            while (responce.Data == null) // пока пустые данные, повторно выполнять запрос
            {
                if (string.IsNullOrEmpty(node) || (responce.Data == null && responce.ErrorData == null && responce.Error == null))
                {
                    return null;
                }
                responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload);
            }

            var pid = JsonConvert.DeserializeObject<QemuGuestCommandResponceDTO>(responce.Data?.ToString()!)?.Pid ?? 0; // получение id задачи для контроля её выполнения

            var result = await GetCommandResult(vmId, currentProxmox, pid); // получение результата выполнения

            while (result == null) // если результат пустой
            {
                responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload); // повторная отправка запроса с командой
                pid = JsonConvert.DeserializeObject<QemuGuestCommandResponceDTO>(responce.Data?.ToString()!)?.Pid ?? 0;
                result = await GetCommandResult(vmId, currentProxmox, pid); // ожидание результата
            }

            return (bool)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + "\n" + ex.InnerException?.Message);
            return null;
        }
    }

    /// <summary>
    /// Получение результата выполнения команды
    /// </summary>
    /// <param name="vmId">iD of VM</param>
    /// <param name="currentProxmox">подключения к Proxmox из БД</param>
    /// <param name="pid">ID of proccess to control</param>
    /// <returns>nullabe bool with result</returns>
    private async Task<bool?> GetCommandResult(int vmId, ProxmoxModel currentProxmox, int pid)
    {
        try
        {
            var nodes = await GetProxmoxNodesListAsync(currentProxmox.Id); // получить список всех хостов
            var nodeList = nodes.Select(x => x.Node);
            string node = await GetNodeName(currentProxmox, vmId); // получить имя хоста на котором машина

            var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec-status?pid={pid}"; // URL команда проверки команды

            while (true)
            {
                var result = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken); // отправка запроса на PRoxmox
                var resultData = result.Data;                

                if (result.ErrorData != null ) // если есть ошибка, прерывать
                {
                    break;
                }

                if (result.Error == null && result.Data == null && result.ErrorData == null) // если ответ полностью пустой
                {
                    return null;
                }

                var res = JsonConvert.DeserializeObject<QemuGuestStatusResponceDTO>(result!.Data!.ToString()!);

                if (res != null && res.ExitCode == 0 && res.Exited /*&& !string.IsNullOrEmpty(res.OutPut)*/)     // если всё корректно
                {
                    return true;
                }
                else if (res != null && !res.Exited) // если задача ещё не завершилась
                {
                    await Task.Delay(5000);
                    continue;
                }
                else if (res != null && res.Exited && res.ExitCode == 1) // если ошибка
                {
                    await Task.Delay(5000);
                    return null;
                }
                else if (res != null && res.Exited && res.ExitCode == 100) // если устранимая типичная 100 ошибка
                {
                    if (res.OutPut != null && res.OutPut.Contains("Something wicked happened resolving"))
                    {
                        return false;
                    }
                    await SendQemuGuestCommand(vmId, currentProxmox, "sudo dpkg --configure -a"); // повторная отправка команды
                }
                else
                {
                    _logger.LogError($"Error in {vmId}, Error code = {res.ExitCode}");
                    return false;
                }
            }

            _logger.LogError($"Error in {vmId}");
            return false;
        }
        catch(NullReferenceException ex)
        {
            _logger.LogError($"Error in {vmId}\n{ex.Message}");
            return null;
        }
        catch(IOException ex)
        {
            _logger.LogError($"Error in {vmId}\n{ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in {vmId}\n{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// получение имени Proxmox хоста на котором развёрнута VM
    /// </summary>
    /// <param name="proxmox">подключения к Proxmox из БД</param>
    /// <param name="vmId">Id of VM</param>
    /// <returns>Имя хоста</returns>
    private async Task<string> GetNodeName(ProxmoxModel proxmox, int vmId)
    {
        var nodes = await GetProxmoxNodesListAsync(proxmox.Id); // получение списка хостов
        var nodeList = nodes.Select(x => x.Node);
       
        // для каждого хоста порверка наличия машины
        foreach (var item in nodeList)
        {
            var url = proxmox?.ProxmoxURL + $"/api2/json/nodes/{item}/qemu/{vmId}/config";
            var res = (await SendRequestToProxmoxAsync(url, HttpMethod.Get, proxmox.ProxmoxToken)).Data;

            if (res != null)
            {
                return item;
            }
        }
        return "";
    }

    // Замена данных в команде подключения к Rancher с учётоп параметров для прокси
    private string SetLinuxConnectCommand(string connectionString, string typeConnestionTo)
    {
        var http_proxy = _configuration["HTTP_PROXY"] ?? "http://10.254.49.150:3128";
        var https_proxy = _configuration["HTTPS_PROXY"] ?? "http://10.254.49.150:3128";
        var no_proxy = _configuration["NO_PROXY"] ?? "127.0.0.0/8,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,.svc,.cluster.local,rancher.a1by.tech,.main.velcom.by";

        connectionString = connectionString.Replace("sudo sh", $"sudo HTTP_PROXY=\"{http_proxy}\" HTTPS_PROXY=\"{https_proxy}\" NO_PROXY=\"{no_proxy}\" sh");
        var result = connectionString + typeConnestionTo;
        return result;
    }

    /// <summary>
    /// переконфигурация машины
    /// </summary>
    /// <param name="proxmoxModel">подключения к Proxmox из БД</param>
    /// <param name="currentNode">Выбранный хост Proxmox</param>
    /// <param name="vmId">ID of VM</param>
    /// <param name="vmConfig">Выбранная конфигурация</param>
    /// <param name="vlanTag">Номер сети для развётрывания</param>
    /// <returns></returns>
    private async Task Reconfigure(ProxmoxModel proxmoxModel, string currentNode, int vmId, TemplateParams vmConfig, int? vlanTag = default)
    {
        try
        {
            var info = await GetVmInfoAsync(proxmoxModel.Id, vmId); // получение данных о машине

            // выставение нужных параметров
            var cpu = int.Parse(vmConfig.CPU);
            var templateParam = double.Parse(vmConfig.HDD.Replace(',', '.'), CultureInfo.InvariantCulture);
            var currHDDSize = double.Round(FormatBytes(info.MaxDisk), 1);

            var url = $"{proxmoxModel.ProxmoxURL}/api2/json/nodes/{currentNode}/qemu/{vmId}/config"; // формирование URL
            var res = JObject.Parse((await SendRequestToProxmoxAsync(url, HttpMethod.Get, proxmoxModel.ProxmoxToken)).Data.ToString()); // отправка запорса и конвертация ответа
            KeyValuePair<string, JToken?> net = default;

            // поиск объёкта отвечающего за сеть
            foreach (var i in res)
            {
                if (i.Key.Contains("net"))
                {
                    net = i;
                    break;
                }
            }

            var newNetTag = string.Empty;
            if (net.Key != default)
            {
                var netString = net.Value.ToString();
                newNetTag = $", \"{net.Key}\": \"{netString[..(netString.IndexOf("tag=") + 4)] + vlanTag}\" "; // выставление нужного номера сети
            }
            

            if (info.CPUS != cpu || currHDDSize != templateParam) // если параметры в шаблоне не сообветствуют текущим параметрам машины
            {
                // setting VM Params 
                var newConfig = new { sockets = 1, cores = cpu, vcpus = cpu, memory = double.Parse(vmConfig.RAM, CultureInfo.InvariantCulture) * 1024 };
                
                var payload = JsonConvert.SerializeObject(newConfig); // конвертация в нагрузку

                if (!string.IsNullOrEmpty(newNetTag))
                {
                    payload = payload[..(payload.LastIndexOf('}') - 2)] + newNetTag + '}';
                }
                
                await SendRequestToProxmoxAsync(url, HttpMethod.Post, proxmoxModel.ProxmoxToken, payload); // отправка запроса

                // Setting New Vm Disk size
                var incrementSize = double.Parse(vmConfig.HDD.Replace(',', '.'), CultureInfo.InvariantCulture) - double.Round( FormatBytes(info.MaxDisk), 1); // высчитывание увеличения разменра для HDD каждой из машин
                
                if (incrementSize < 1)
                {
                    return;
                }

                var newSize = new { Disk = "scsi0", Size = $"+{incrementSize}G" };
                payload = JsonConvert.SerializeObject(newSize);

                url = $"{proxmoxModel.ProxmoxURL}/api2/json/nodes/{currentNode}/qemu/{vmId}/resize"; //  формирование URL
                await SendRequestToProxmoxAsync(url, HttpMethod.Put, proxmoxModel.ProxmoxToken, payload); // отправка команды на изменение размера HDD
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in {nameof(Reconfigure)}\n{ex.Message}");
        }
    }

    /// <summary>
    /// Отправка группы команд для исполнения в машину
    /// </summary>
    /// <param name="vmId">Id og vm </param>
    /// <param name="currentProxmox">подключения к Proxmox из БД</param>
    /// <param name="list">Список команд для мсполнения</param>
    /// <returns>Результат исполнения всех команд</returns>
    private async Task<bool> SendGroupOfCommands(int vmId, ProxmoxModel currentProxmox, List<string> list)
    {
        var resultList = new List<bool>();

        // для каждой команды в списке
        foreach (var command in list)
        {
            var readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, command); // отправка запроса
            while (readyStatus == null) // пока нет статуса
            {
                readyStatus = await SendQemuGuestCommand(vmId, currentProxmox, command); // отправка запроса
            }

            resultList.Add((bool)readyStatus); // добавление в результата в общий список

            if (command.Contains("reboot")) // если перезагрузка , ожидать минуту
            {                
                await Task.Delay(1 * 60 * 1_000);
            }
        }

        if (resultList.Contains(false)) // если есть хоть одна ошибка, то общый результат - манина не готова
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Проверка минимальных требований
    /// </summary>
    /// <param name="clusterInfo">Параметры для создания кластера машин</param>
    private void CheckMinimalRequarements(CreateVMsDTO clusterInfo)
    {
        int.TryParse(_configuration["CPUMin"] ?? "2", out int minCPU);
        double.TryParse(_configuration["RAMMin"] ?? "4", out double minRAM);

        var workerCPU = int.Parse(clusterInfo.VMConfig.CPU, CultureInfo.InvariantCulture); 
        var workerRAM = double.Parse(clusterInfo.VMConfig.RAM.Replace(',', '.'), CultureInfo.InvariantCulture);

        var etcdCPU = int.Parse(clusterInfo.etcdConfig.CPU, CultureInfo.InvariantCulture);
        var etcdRAM = double.Parse(clusterInfo.etcdConfig.RAM.Replace(',', '.'), CultureInfo.InvariantCulture);

        if (workerCPU < minCPU) // если количкество CPU рабочих узлов меньше минимального
        {
            clusterInfo.VMConfig.CPU = _configuration["CPUMin"] ?? "2";
        }

        if (etcdCPU < minCPU) // если количкество CPU управляющих узлов меньше минимального
        {
            clusterInfo.etcdConfig.CPU = _configuration["CPUMin"] ?? "2";
        }

        if (workerRAM < minRAM) // если количкество RAM рабочих узлов меньше минимального
        {
            clusterInfo.VMConfig.RAM = _configuration["RAMMin"] ?? "4";
        }

        if (etcdRAM < minRAM)// если количкество RAM управляющих узлов меньше минимального
        {
            clusterInfo.etcdConfig.RAM = _configuration["RAMMin"] ?? "4";
        }
    }

    // Преобразовать список объектов в стоку
    private List<object> ConvertToListString(List<object>? source)
    {
        var results = new List<object>();

        if (source == null) // если список пуст
        {
            return new List<object>();
        }

        foreach (var result in source)
        {
            results.Add(result);

            _logger.LogInformation("Created VM = " + result.ToString());
        }

        return results;
    }
}