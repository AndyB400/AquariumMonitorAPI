﻿using AquariumMonitor.API.Controllers;
using AquariumMonitor.Models;
using AquariumMonitor.Models.ViewModels;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AquariumMonitor.API.Models
{
    public class AquariumUrlResolver : IValueResolver<Aquarium, AquariumModel, string>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AquariumUrlResolver(IHttpContextAccessor httpContextAccessor )
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Resolve(Aquarium source, AquariumModel destination, string destMember, ResolutionContext context)
        {
            var url = (IUrlHelper)_httpContextAccessor.HttpContext.Items[BaseController.URLHELPER];
            return url.Link("AquariumGet", new { aquariumId = source.Id });
        }
    }
}
