﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using WebApplication1.Models;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;

namespace WebApplication1.Data.Database;

public class DBWorker : IDBService // класс обслуживающий взаимодействе с БД
{
    private readonly MainDBContext _dbContext;
    private readonly ILogger<DBWorker> _logger;

    public DBWorker(MainDBContext dBContext, ILogger<DBWorker> logger)
    {
        _dbContext = dBContext;
        _logger = logger;
    }

    public async Task<bool> CheckDBConnection() // проверка подключения к БД
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

    public async Task<bool> AddNewCred(object model) // добавление нового подключения в БД
    {
        try
        {
            if (model is ProxmoxModel) // если тип подключения Proxmox
            {
                _logger.LogInformation("Adding new Proxmox Creds");
                if (await _dbContext.Proxmox.Where(x =>
                    x.ProxmoxURL == (model as ProxmoxModel).ProxmoxURL &&
                    x.ProxmoxToken == (model as ProxmoxModel).ProxmoxToken)
                    .AnyAsync())
                {
                    throw new ArgumentException("This creds already exist", nameof(model));
                }

                await _dbContext.Proxmox.AddAsync(model as ProxmoxModel);
            }
            else if (model is RancherModel) // если тип подключения Rancher
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

    /// <summary>
    /// Returns List of connection creds of specified connection type 
    /// </summary>
    /// <param name="connectionType">object of ConnectionType</param>
    /// <returns>List of enum ConnectionType</returns>
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

    /// <summary>
    /// Updates records in DB associated with connection types
    /// </summary>
    /// <param name="model">Represents enum of Connection type</param>
    /// <returns> Bool value about success of updating value in DB</returns>
    public async Task<bool> Update(object model)
    {
        try
        {
            if (model is ProxmoxModel pModel)
            {
                _dbContext.Proxmox.Update(pModel);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            else if (model is RancherModel rModel)
            {
                _dbContext.Rancher.Update(rModel);
                await _dbContext.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }
}