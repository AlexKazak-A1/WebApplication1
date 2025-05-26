using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Data.Services;

public class ProvisionService : IProvisionService
{
    private readonly ILogger<ProvisionService> _logger;
    private readonly IRancherService _rancherService;
    private readonly IProxmoxService _proxmoxService;
    private readonly IDBService _dbWorker;
    private readonly IConfiguration _configuration;

    public ProvisionService(ILogger<ProvisionService> logger, IRancherService rancherService, IProxmoxService proxmoxService, IDBService dBService, IConfiguration configuration)
    {
        _logger = logger;
        _dbWorker = dBService;
        _proxmoxService = proxmoxService;
        _rancherService = rancherService;
        _configuration = configuration;
    }

    /// <summary>
    /// Получение данных для подключения из БД
    /// </summary>
    /// <param name="connectionTarget">Тип необходитого подключения</param>
    /// <returns>Возвращает Json, содержащий список доступных подключений</returns>
    public async Task<JsonResult> GetConnectionCreds(ConnectionType connectionTarget)
    {
        try
        {
            // Attempt to parse connectionTarget into the ConnectionType enum
            //var type = Enum.Parse<ConnectionType>(connectionTarget.ToString(), true);

            // Process the valid connection type
            var result = await GetCreds(connectionTarget);
            return new JsonResult(result);
        }
        catch (ArgumentException)
        {
            // Return error if connectionTarget is not a valid ConnectionType
            return new JsonResult(new Response { Status = Status.ERROR, Message = "No such connection type" });
        }
    }

    /// <summary>
    /// Создание кластера Rancher
    /// </summary>
    /// <param name="param">Объект с параметрами для создания кластера</param>
    /// <returns>Возврат информаци об успешности создания кластера</returns>
    /// <exception cref="Exception">Если возникает любая ошибка</exception>
    public async Task<JsonResult> CreateClaster(CreateClusterDTO param)
    {
        if (param == null) // проверка валидности типа
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Wrong type in CreateClaster" });
        }

        try
        {

            var creationResult = await _rancherService.CreateClusterAsync(param); // создание кластера

            if (creationResult != null)
            {
                return creationResult;
            }            

            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}" });
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}");
        }
    }

    /// <summary>
    /// Создание VM в Proxmox
    /// </summary>
    /// <param name="param">Параметры для создания машин</param>
    /// <returns>JSon сщ списком машин и результатом их создания</returns>
    /// <exception cref="Exception">Если возникает любая ошибка</exception>
    public async Task<JsonResult> CreateProxmoxVMs(CreateVMsDTO param)
    {
        try
        {
            var responseList = new List<Response>();

            //var param = JsonConvert.DeserializeObject<CreateVMsDTO>(data.ToString());
            //if (!await _proxmoxService.CheckCreationAbility(param))
            //{
            //    responseList.Add(new Response { Status = Status.ERROR, Message = "Can not create vms, because of resource limit" });
            //}
            var creationVMsResult = await _proxmoxService.StartProvisioningVMsAsync(param); // Начало создания машин

           

            // формирование результирующего списка машин и их результатов
            foreach (var str in creationVMsResult as List<object>)
            {
                if (str is int vmId)
                {
                    responseList.Add(new Response { Status = Status.OK, Message = vmId.ToString() });
                }
                else
                {
                    if (str is string response && response.Contains("already exist"))
                    {
                        responseList.Add(new Response { Status = Status.ALREADY_EXIST, Message = response.Split(' ')[0] });
                    }
                    else
                    {
                        responseList.Add(new Response { Status = Status.WARNING, Message = str.ToString() });
                    }
                }
            }

            return new JsonResult(responseList);
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(CreateProxmoxVMs)} in {nameof(ProvisionService)}", ex);
        }
    }

    /// <summary>
    /// Получение строки подключения к Rancher
    /// </summary>
    /// <param name="clusterInfo">Параметры созданного кластера Rancher</param>
    /// <returns>Json с результатом</returns>
    /// <exception cref="Exception">Если возникает любая ошибка</exception>
    public async Task<JsonResult> GetConnectionStringToRancher(CreateClusterDTO clusterInfo)
    {
        if (clusterInfo == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster Name not provided!" });
        }

        try
        {
            var connectionString = await _rancherService.GetConnectionString(clusterInfo.RancherId, clusterInfo.ClusterName); // Получение строки подключения 
            return new JsonResult(new Response { Status = Status.OK, Message = connectionString });
        }
        catch (Exception ex)
        {
           throw new Exception($"Check {nameof(GetConnectionStringToRancher)} in {nameof(ProvisionService)}", ex);
        }
    }

    /// <summary>
    /// Запуск VM и подключение их к Rancher
    /// </summary>
    /// <param name="data">Объект с данными для подключения</param>
    /// <returns>Json с результатом подключения</returns>
    /// <exception cref="Exception">Если возникает любая ошибка</exception>
    public async Task<JsonResult> StartVMAndConnectToRancher(ConnectVmToRancherDTO data)
    {
        // проверка валидности данных
        if (data == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Input data can't be null" });
        }


        if (!int.TryParse(data.ProxmoxId, out int currentProxmoxId))
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Wrong type of ProxmoxId" });
        }

        try
        {
            // Запуск машин
            var vmsRunningState = await _proxmoxService.StartVmsAsync(data.VMsId, currentProxmoxId);

            // Ожидание статуса готовности машин к подключению к Rancher
            var vmsReadyStatus = await _proxmoxService.WaitReadyStatusAsync(vmsRunningState, currentProxmoxId, data.ConnectionString);

            // формирование результата
            List<Response> result = new();
            foreach (var vmStatus in vmsReadyStatus)
            {
               if (vmStatus.Value) // машина успешно подключена
               {
                    result.Add(new Response { Status = Status.OK, Message = vmStatus.Key.ToString() });
               }
               else // возникла проблема подключения
               {
                    result.Add(new Response { Status = Status.WARNING, Message = vmStatus.Key.ToString() });
               }
            }

            return new JsonResult(result);
            //return new JsonResult(new Response { Status = Status.OK, Message = "test ready status check is Ok" });
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(StartVMAndConnectToRancher)} in {nameof(ProvisionService)}", ex);
        }
    }
    
    /// <summary>
    /// Получение параметров подключения из БД(общий метод)
    /// </summary>
    /// <param name="inputType">Тип подключения</param>
    /// <returns>Список с доступными подключениями</returns>
    private async Task<List<SelectOptionDTO>> GetCreds(object inputType)
    {
        var selectList = new List<SelectOptionDTO>();

        if (inputType is ConnectionType type) // проверка типа
        {
            if (type == ConnectionType.Proxmox) // если это Proxmox
            {
                var currentTypeArray = new List<ProxmoxModel>((List<ProxmoxModel>)await _dbWorker.GetConnectionCredsAsync(type)); // получение данных из БД

                // добавление в список
                foreach (var proxmox in currentTypeArray)
                {
                    selectList.Add(new SelectOptionDTO
                    {
                        Value = proxmox.Id.ToString(),
                        Text = proxmox.ProxmoxURL,
                    });
                }

            }
            else if (type == ConnectionType.Rancher) // если это Rancher
            {
                var currentTypeArray = new List<RancherModel>((List<RancherModel>)await _dbWorker.GetConnectionCredsAsync(type)); // получение данных из БД

                // добавление в список
                foreach (var rancher in currentTypeArray)
                {
                    selectList.Add(new SelectOptionDTO
                    {
                        Value = rancher.Id.ToString(),
                        Text = rancher.RancherURL,
                    });
                }

            }
        }

        return selectList;
    }    

    /// <summary>
    /// Проверка возможности создания VM с указанными параметрами
    /// </summary>
    /// <param name="info">Параметры для создания VM</param>
    /// <returns>JSON с информацией о возможности создания кластера</returns>
    public async Task<JsonResult> GetCreationAvailibility(CreateVMsDTO info)
    {
        
        if (info == null) // проверка валидности
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"info is null in {nameof(GetCreationAvailibility)}" });
        }

        try
        {
            var vmAllocation = new Dictionary<string, string>();
            var result = await _proxmoxService.CheckCreationAbility(info); // проверка возможности создания кластера с учётом переподписки и параметров по умолчанию

            if (result != null)
            {
                return new JsonResult(new Response { Status = Status.OK, Message = "Cluster can be Created", Data = result });
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster can`t be Created" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Error in in {nameof(GetCreationAvailibility)}" });
        }
    }
}
