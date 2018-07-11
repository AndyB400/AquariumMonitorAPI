using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AquariumMonitor.API.Filters;
using AquariumMonitor.BusinessLogic.Interfaces;
using AquariumMonitor.DAL.Interfaces;
using AquariumMonitor.Models.ViewModels;
using AutoMapper;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace AquariumMonitor.API.Controllers
{
    /*
     *Controller used for web ui testing 
     * 
     */
    [Produces("application/json")]
    [Route("api/aqua")]
    [ValidateModel]
    public class AquaController : BaseController
    {
        private readonly IAquariumRepository _repository;
        private readonly IUnitManager _unitManager;
        private readonly IAquariumTypeManager _aquariumTypeManager;

        public AquaController(IAquariumRepository repository,
            ILoggerAdapter<BaseController> logger,
            IMapper mapper,
            IUnitManager unitManager,
            IAquariumTypeManager aquariumTypeManager) : base(logger, mapper)
        {
            _repository = repository;
            _unitManager = unitManager;
            _aquariumTypeManager = aquariumTypeManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var aquariums = await _repository.GetForUser(1);

            return Ok(Mapper.Map<IEnumerable<AquariumModel>>(aquariums));
        }

        [AllowAnonymous]
        [HttpGet("{aquariumId}", Name = "AquaGet")]
        public async Task<IActionResult> Get(int aquariumId)
        {
            var aquarium = await _repository.Get(1, aquariumId);

            if (aquarium == null)
            {
                Logger.Warning($"Aquarium not found. AquariumID: {aquariumId}");
                return NotFound();
            }

            AddETag(aquarium.RowVersion);

            return Ok(Mapper.Map<AquariumModel>(aquarium));
        }
    }
}