using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AquariumMonitor.API.Filters;
using AquariumMonitor.BusinessLogic.Interfaces;
using AquariumMonitor.DAL.Interfaces;
using AquariumMonitor.Models;
using AquariumMonitor.Models.ViewModels;
using AutoMapper;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

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

        // POST: api/aqua
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Post([FromBody]AquariumModel model)
        {
            if (model == null) return BadRequest("Aquarium cannot be null.");

            try
            {
                var aquarium = Mapper.Map<Aquarium>(model);

                if (aquarium == null)
                {
                    return UnprocessableEntity();
                }

                // Use the URL User Id
                if (aquarium.User == null) aquarium.User = new User();
                aquarium.User.Id = UserId;

                await LookupTypeAndUnits(aquarium);

                Logger.Information("Creating new aquarium...");
                await _repository.Add(aquarium);
                Logger.Information($"New aquarium created. AquariumID:{aquarium.Id}.");

                AddETag(aquarium.RowVersion);

                var url = Url.Link("AquariumGet", new { UserId, aquariumId = aquarium.Id });
                return Created(url, Mapper.Map<AquariumModel>(aquarium));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured whilst trying to create Aquarium.");
            }
            return BadRequest("Could not create Aquarium");
        }

        // PUT: api/aqua/5
        [AllowAnonymous]
        [HttpPut("{aquariumId}")]
        public async Task<IActionResult> Put(int aquariumId, [FromBody]AquariumModel model)
        {
            if (model == null) return BadRequest("Aquarium cannot be null.");

            try
            {
                var aquarium = await _repository.Get(UserId, aquariumId);
                if (aquarium == null)
                {
                    Logger.Warning($"Aquarium not found. AquariumID: {aquariumId}");
                    return NotFound();
                }

                if (Request.Headers.ContainsKey(HeaderNames.IfMatch))
                {
                    var etag = Request.Headers[HeaderNames.IfMatch].First();
                    if (etag != Convert.ToBase64String(aquarium.RowVersion))
                    {
                        return StatusCode((int)HttpStatusCode.PreconditionFailed);
                    }
                }

                Mapper.Map(model, aquarium);

                await LookupTypeAndUnits(aquarium);

                Logger.Information($"Updating aquarium. AquariumID:{aquariumId}");
                await _repository.Update(aquarium);

                AddETag(aquarium.RowVersion);

                return Ok(Mapper.Map<AquariumModel>(aquarium));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occured whilst trying to update Aquarium");
            }
            return BadRequest("Could not update Aquarium");
        }

        private async Task LookupTypeAndUnits(Aquarium aquarium)
        {
            // Lookup units
            aquarium.DimensionUnit = await _unitManager.LookUpByName(aquarium.DimensionUnit);
            aquarium.VolumeUnit = await _unitManager.LookUpByName(aquarium.VolumeUnit);

            //Look Up Type
            aquarium.Type = _aquariumTypeManager.LookupFromName(aquarium.Type);
        }
    }
}