﻿using Dapper;
using AquariumMonitor.DAL.Interfaces;
using AquariumMonitor.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AquariumMonitor.DAL
{
    public class UnitRepository : BaseRepository, IUnitRepository
    {
        private const string GetByNameQuery = @"SELECT Id, Name FROM dbo.Units WHERE Name = @name";

        public UnitRepository(IConnectionFactory connectionFactory,
            ILogger<UnitRepository> logger) : base(connectionFactory, logger)
        {
            
        }

        public async Task<Unit> GetUnitFromName(string name)
        {
            Unit unit;

            using (var connection = _connectionFactory.GetOpenConnection())
            {
                unit = await connection.QueryFirstOrDefaultAsync<Unit>(GetByNameQuery, new { name });
            }
            return unit;
        }
    }
}
