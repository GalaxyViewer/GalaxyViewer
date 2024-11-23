using LiteDB;
using System.Collections.Generic;
using System.Linq;
using GalaxyViewer.Models;

namespace GalaxyViewer.Services
{
    public class GridService
    {
        private const string _databasePath = "Filename=Grids.db; Connection=shared";

        public void AddGrid(GridModel grid)
        {
            using var db = new LiteDatabase(_databasePath);
            var grids = db.GetCollection<GridModel>("grids");
            grids.Insert(grid);
        }

        public List<GridModel> GetAllGrids()
        {
            using var db = new LiteDatabase(_databasePath);
            var grids = db.GetCollection<GridModel>("grids");
            return grids.FindAll().ToList();
        }
    }
}