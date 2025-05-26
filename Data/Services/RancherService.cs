using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Newtonsoft.Json;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;
using System.Text;
using WebApplication1.Data.WEB;

namespace WebApplication1.Data.Services;

/// <summary>
/// Обслуживает большенство действий необходитых для работы с Rancher
/// </summary>
public class RancherService : IRancherService
{
    private readonly IDBService _dbWorker;
    private readonly ILogger<RancherService> _logger;
    private readonly IConfiguration _configuration;

    public RancherService (ILogger<RancherService> logger, IDBService dbWorker, IConfiguration configuration)
    {
        _logger = logger;
        _dbWorker = dbWorker;
        _configuration = configuration;
    }

    /// <summary>
    /// Создание нового подключения к Rancher в БД
    /// </summary>
    /// <param name="model">Модель с параметрами подключения</param>
    /// <returns>Json с результатом добавления</returns>
    public async Task<JsonResult> CreateNewRancherCred([FromBody] RancherModel model)
    {
        if (string.IsNullOrEmpty(model.RancherURL)) // Пустой URL
        {
            return new JsonResult(new { Status = Status.WARNING, Message = "Rancher url is empty" });
        }

        if (string.IsNullOrEmpty(model.RancherToken)) // пустой токен
        {
            return new JsonResult(new { Status = Status.WARNING, Message = "Rancher Token is empty" });
        }

        if (!await _dbWorker.CheckDBConnection()) // проверка наличия подключения к БД
        {
            return new JsonResult(new { Status = Status.ERROR, Message = "Database is unreachable" });
        }

        if (await _dbWorker.AddNewCred(model)) // добавление нового подключения
        {
            return new JsonResult(new { Status = Status.OK, Message = "New Rancher Creds was successfully added" });
        }

        return new JsonResult(new
        {
            Status = Status.WARNING,
            Message = "New Rancher Creds wasn`t added.\n" +
            "Maybe you try to add existing data.\n Contact an adnimistrator"
        });
    }

    /// <summary>
    /// Получение Строки подключения к Rancher
    /// </summary>
    /// <param name="RancherId"> ID of rancher connection in DB</param>
    /// <param name="ClusterName">Name of selected cluster</param>
    /// <returns>Connection string</returns>
    public async Task<string> GetConnectionString(string RancherId, string ClusterName)
    {
        if (RancherId == null || ClusterName == null) // validate input values
        {
            return string.Empty;
        }

        try
        {
            var currentRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).First(x => x.Id == int.Parse(RancherId)); // получение данных по ID fron DB

            await Task.Delay(1500);
            var clusterId = await GetCurrentClusterID(currentRancher.RancherURL, currentRancher.RancherToken, ClusterName); // получение ID кластера в системе Rancher с указанным именем

            var insecureConnString = await GetInsecureConnectionString(currentRancher.RancherURL, currentRancher.RancherToken, clusterId); // получение строки подключения выбранного кластера


            return insecureConnString;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Создание кластера Rancher
    /// </summary>
    /// <param name="clusterInfo">Информация для создания кластера</param>
    /// <returns>JSON с результатом создания</returns>
    public async Task<JsonResult> CreateClusterAsync(CreateClusterDTO clusterInfo)
    {
        // выставление нагрузки для создания кластера
        var payload = SetPayload((clusterInfo)?.ClusterName);

        //Ignore certificate checking
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var httpClient = new HttpClient(handler);

        var selectedRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).Where(x => x.Id == int.Parse(clusterInfo.RancherId)).First(); // получение параметров подключения из БД
        var url = selectedRancher.RancherURL + "/v1/provisioning.cattle.io.clusters"; // добавление пути для создания кластера

        // Create the content with the JSON payload
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", selectedRancher.RancherToken); // добавление авторизации

        var response = await httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return new JsonResult(new Response { Status = Status.OK, Message = "Cluster created successfully" });
        }
        else // если ошибка
        {
            var resultTest = JsonConvert.DeserializeObject<RancherResponse>(await response.Content.ReadAsStringAsync()); // разбор отвера от Rancher
            if (resultTest!.Code.Equals("AlreadyExists"))
            {
                return new JsonResult(new Response { Status = Status.ALREADY_EXIST, Message = resultTest.Message });
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = resultTest.Message });
        }

    }

    /// <summary>
    /// Получение Параметров по уникальному имени
    /// </summary>
    /// <param name="uniqueRancherName">Уникальное имя Rancher</param>
    /// <returns></returns>
    public async Task<int> GetRancherCred(string uniqueRancherName)
    {
        if (!await _dbWorker.CheckDBConnection()) // проверка подключения к БД
        {
            return -1;
        }

        var rancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).Where(x => x.RancherUniqueName == uniqueRancherName).FirstOrDefault(); // получение ID по имени
        if ( rancher != null)
        {
            return rancher.Id;
        }
        
        return -1;
    }

    /// <summary>
    /// Получение списка всех доступных подключений из БД
    /// </summary>
    /// <returns>Список подключений к Rancher</returns>
    /// <exception cref="NullReferenceException">Если БД не доступна</exception>
    public async Task<List<RancherModel>> GetAllRancher()
    {
        if (!await _dbWorker.CheckDBConnection()) // проверка доступности БД
        {
            throw new NullReferenceException();
        }

        var allRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher)) as List<RancherModel>; // получение списка подключений

        return allRancher ?? new List<RancherModel>();
    }

    /// <summary>
    /// Обновление данных о подключении Rancher в БД
    /// </summary>
    /// <param name="reconfig">Обновлённые параметры</param>
    /// <returns>Bool value of update</returns>
    /// <exception cref="NullReferenceException">Если БД не доступна</exception>
    public async Task<bool> UpdateRancher([FromBody] RancherReconfigDTO reconfig)
    {
        if (!await _dbWorker.CheckDBConnection()) // проверка доступности БД
        {
            throw new NullReferenceException();
        }

        var currentRancher = ((await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher)) as List<RancherModel>).Where(x => x.Id == reconfig.Id).FirstOrDefault(); // получение записис указанным ID

        if (currentRancher == null) // если записи нет
        {
            return false;
        }

        currentRancher.RancherUniqueName = reconfig.UniqueRancherName; // обновление имени

        if (await _dbWorker.Update(currentRancher)) // обновление данных в БД
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Получение ID of Runcher кластера в системе Rancher
    /// </summary>
    /// <param name="url">Адрес подключения</param>
    /// <param name="token">Токен подключения</param>
    /// <param name="clusterName">Имя кластера</param>
    /// <returns>Уникальный идентификатор в системе Rancher</returns>
    private async Task<string> GetCurrentClusterID(string url, string token, string clusterName)
    {
        url += "/v1/provisioning.cattle.io.clusters?exclude=metadata.managedFields"; // выставление пути запроса
        var response = await PostToRancher(url, token); // отправка запроса на Rancher
        var result = JsonConvert.DeserializeObject<List<ClusterStatusDTO>>(response);

        // поиск ID кластера
        foreach (var item in result)
        {
            if (item.Metada.Name.Equals(clusterName))
            {
                return item.Status.ClusterName;
            }
        }

        return "";
    }

    /// <summary>
    /// Получение стоки подключения к Rancher cluster
    /// </summary>
    /// <param name="url">Адрес подключения</param>
    /// <param name="token">Токен подключения</param>
    /// <param name="clusterID">Идентификатор кластера в системе Rancher</param>
    /// <returns>Строку подключения</returns>
    private async Task<string> GetInsecureConnectionString(string url, string token, string clusterID)
    {
        url += "/v3/clusterregistrationtokens"; // выставление пути
        var responce = await PostToRancher(url, token); // запрос к Rancher
        var data = JsonConvert.DeserializeObject<List<RancherClucterRegistrationDTO>>(responce);

        return data.Where(x => x.ClusterId.Equals(clusterID)).FirstOrDefault().InsecureNodeCommand;
    }

    /// <summary>
    /// Запрос к Rancher
    /// </summary>
    /// <param name="url">Адрес подключения</param>
    /// <param name="token">Токен подключения</param>
    /// <returns>Строку с результатом исполнения запроса</returns>
    private async Task<string> PostToRancher(string url, string token)
    {
        var handler = new HttpClientHandler // игнорирование сертификатов
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        // добавление авторизации
        using var httpClient = new HttpClient(handler);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


        var response = await httpClient.SendAsync(request);
        var resultTest = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RancherResponse>(resultTest);
        return result.Data.ToString();
    }

    /// <summary>
    /// Установка полезной нагрузки при создании кластера
    /// </summary>
    /// <param name="newClusterName">Имя создаваемого кластера</param>
    /// <returns>Строку с правильной нагрузкой</returns>
    private string SetPayload(string newClusterName = "NewClusterName")
    {
        // Updating name of new cluster
        var stringJson = "{\"type\":\"provisioning.cattle.io.cluster\",\"metadata\":{\"namespace\":\"fleet-default\",\"name\":\"NameHere\"},\"spec\":{\"rkeConfig\":{\"chartValues\":{\"rke2-calico\":{}},\"upgradeStrategy\":{\"controlPlaneConcurrency\":\"1\",\"controlPlaneDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120},\"workerConcurrency\":\"1\",\"workerDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120}},\"machineGlobalConfig\":{\"cni\":\"calico\",\"disable-kube-proxy\":false,\"etcd-expose-metrics\":false},\"machineSelectorConfig\":[{\"config\":{\"protect-kernel-defaults\":false}}],\"etcd\":{\"disableSnapshots\":false,\"s3\":null,\"snapshotRetention\":5,\"snapshotScheduleCron\":\"0 */5 * * *\"},\"registries\":{\"configs\":{},\"mirrors\":{}},\"machinePools\":[]},\"machineSelectorConfig\":[{\"config\":{}}],\"kubernetesVersion\":\"v1.26.15+rke2r1\",\"defaultPodSecurityPolicyTemplateName\":\"\",\"defaultPodSecurityAdmissionConfigurationTemplateName\":\"\",\"localClusterAuthEndpoint\":{\"enabled\":false,\"caCerts\":\"\",\"fqdn\":\"\"},\"agentEnvVars\":[{\"name\":\"HTTP_PROXY\",\"value\":\"HTTP_PROXY_VALUE\"},{\"name\":\"HTTPS_PROXY\",\"value\":\"HTTPS_PROXY_VALUE\"},{\"name\":\"NO_PROXY\",\"value\":\"NO_PROXY_VALUE\"}]}}";
        stringJson = stringJson.Replace("NameHere", newClusterName); // обновление имени
        stringJson = stringJson.Replace("HTTP_PROXY_VALUE", _configuration["HTTP_PROXY"] ?? "http://10.254.49.150:3128"); // высталение прокси для HTTP
        stringJson = stringJson.Replace("HTTPS_PROXY_VALUE", _configuration["HTTPS_PROXY"] ?? "http://10.254.49.150:3128"); // высталение прокси для HTTPS
        stringJson = stringJson.Replace("NO_PROXY_VALUE", _configuration["NO_PROXY"] ?? "127.0.0.0/8,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,.svc,.cluster.local,rancher.a1by.tech,.main.velcom.by"); // выставление диапазона игнорирования прокси

        return stringJson;
    }
}
