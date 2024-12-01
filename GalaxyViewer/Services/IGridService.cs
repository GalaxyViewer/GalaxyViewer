using System.Collections.Generic;
using GalaxyViewer.Models;

namespace GalaxyViewer.Services
{
    public interface IGridService
    {
        IEnumerable<GridModel> GetAllGrids();
        GridModel GetGridByNick(string gridNick);
    }
}