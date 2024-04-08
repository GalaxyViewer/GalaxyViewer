using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using GalaxyViewer.Models;

namespace GalaxyViewer.Services
{
    public class GridService
    {
        public List<Grid> ParseGridsFromXml()
        {
            string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "grids.xml");
            var xdoc = XDocument.Load(xmlFilePath);
            var grids = new List<Grid>();

            if (xdoc.Root != null)
            {
                foreach (var gridElement in xdoc.Root.Elements("grid"))
                {
                    var grid = new Grid(
                        gridElement.Element("gridnick")?.Value ?? string.Empty,
                        gridElement.Element("gridname")?.Value ?? string.Empty,
                        gridElement.Element("platform")?.Value ?? string.Empty,
                        gridElement.Element("loginuri")?.Value ?? string.Empty,
                        gridElement.Element("loginpage")?.Value ?? string.Empty,
                        gridElement.Element("helperuri")?.Value ?? string.Empty,
                        gridElement.Element("website")?.Value ?? string.Empty,
                        gridElement.Element("support")?.Value ?? string.Empty,
                        gridElement.Element("register")?.Value ?? string.Empty,
                        gridElement.Element("password")?.Value ?? string.Empty,
                        gridElement.Element("version")?.Value ?? string.Empty
                    );

                    grids.Add(grid);
                }
            }

            return grids;
        }
    }
}