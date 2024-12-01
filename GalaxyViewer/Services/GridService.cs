using System.Collections.Generic;
using System.Linq;
using GalaxyViewer.Models;
using LiteDB;

namespace GalaxyViewer.Services
{
    public class GridService : IGridService
    {
        private readonly ILiteDbService _liteDbService;

        public GridService(ILiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
        }

        public IEnumerable<GridModel> GetAllGrids()
        {
            return _liteDbService.Database.GetCollection<GridModel>("grids").FindAll();
        }

        public GridModel GetGridByNick(string gridNick)
        {
            return _liteDbService.Database.GetCollection<GridModel>("grids").FindOne(g => g.GridNick == gridNick);
        }
    }
}