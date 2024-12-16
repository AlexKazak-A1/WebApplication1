using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Data;

public class DBWorker : IProvision
{
    private readonly MainDBContext _dbContext;
    private readonly ILogger<DBWorker> _logger;

    public DBWorker(MainDBContext dBContext, ILogger<DBWorker> logger)
    {
        _dbContext = dBContext;
        _logger = logger;
    }

    public async Task<bool> CheckDBConnection()
    {
        _logger.LogInformation("DBWorker start DB connection test.");
        try
        {
            var item = _dbContext.Proxmox.Select(x => x.Id);
            _logger.LogInformation("Connection Established");
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> AddNewCred(object model)
    {        
        try
        {
            if(model is ProxmoxModel)
            {
                _logger.LogInformation("Adding new Proxmox Creds");
                if (await _dbContext.Proxmox.Where(x => 
                    x.ProxmoxURL == (model as ProxmoxModel).ProxmoxURL &&
                    x.ProxmoxToken == (model as ProxmoxModel).ProxmoxToken)
                    .AnyAsync()) 
                {
                    throw new ArgumentException("This creds already exist",nameof(model));
                }

                await _dbContext.Proxmox.AddAsync(model as ProxmoxModel);
            }
            else if(model is RancherModel)
            {
                _logger.LogInformation("Adding new Rancher Creds");
                if (await _dbContext.Rancher.Where(x =>
                    x.RancherURL == (model as RancherModel).RancherURL &&
                    x.RancherToken == (model as RancherModel).RancherToken)
                    .AnyAsync())
                {
                    throw new ArgumentException("This creds already exist", nameof(model));
                }
                await _dbContext.Rancher.AddAsync(model as RancherModel);
            }
            else
            {
                _logger.LogWarning("Adding error. Wrong input Type.");
                return false;
            }
            
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something happend while adding new cred to db,\n{ex.Message}");
            return false;
        }
    }

    public async Task<object> GetConnectionCredsAsync(object connectionType)
    {
        if (connectionType is ConnectionType type)
        {
            switch (type)
            {
                case ConnectionType.Rancher:
                    {                        
                        return await _dbContext.Rancher.Select(x => x).ToListAsync();
                    }                    

                case ConnectionType.Proxmox:
                    {
                        return await _dbContext.Proxmox.Select(x => x).ToListAsync();
                    }

                default:
                    break;
            }
        }

        return "Wrong Connection type";
    }
}