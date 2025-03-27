﻿using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Jira;
using WebApplication1.Data.ProxmoxDTO;

namespace WebApplication1.Data.Interfaces;

public interface IJiraService
{
    public Task<object?> CreateClusterLazy(JiraCreateClusterRequestDTO data);

    public Task<JsonResult> GetProxmoxInfo(int proxmoxId);

    public Task<VmInfoDTO> GetVMInfo(int proxmoxId, int VMId);


}
