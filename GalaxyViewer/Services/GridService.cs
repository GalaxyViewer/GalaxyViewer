using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using GalaxyViewer.Models;

namespace GalaxyViewer.Services
{
    public class GridService
    {
        private readonly string _databasePath;

        public GridService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GalaxyViewer");
            Directory.CreateDirectory(appDataPath); // Ensure the directory exists
            _databasePath = Path.Combine(appDataPath, "data.db");
        }

        public List<GridModel> GetAllGrids()
        {
            try
            {
                using var db = new LiteDatabase(_databasePath);
                var collection = db.GetCollection<GridModel>("grids");
                return collection.FindAll().ToList();
            }
            catch (IOException ex) when (ex.Message.Contains("Read-only file system"))
            {
                // Handle read-only file system scenario
                Console.Error.WriteLine("Error: The file system is read-only. Please check the file system permissions.");
                return [];
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}