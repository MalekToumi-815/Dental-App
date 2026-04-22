using Dental_App.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Dental_App.Services
{
    public interface IOrdonnanceServiceTemplate
    {
        // --- Calibration (The "Where") ---
        Task SaveCoordinatesAsync(double x, double y);
        Task<Point> GetStartingCoordinatesAsync();

        // --- Template Management (The "Background") ---
        Task<string> SaveTemplateImageAsync(string filePath);
        Task<string> GetTemplatePathAsync();

        // --- The Printing Engine ---
        // This takes the list of meds and the saved coordinates to build the print job
        FixedDocument CreatePrintDocument(IEnumerable<string> medicaments, Point startPoint , bool isPreview);
    }
    public class OrdonnanceServiceTemplate : IOrdonnanceServiceTemplate
    {
        private readonly DentalContext _context;
        private readonly string _appDataRoot;

        public OrdonnanceServiceTemplate(DentalContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _appDataRoot = Path.Combine(documents, "DentalApp_Data");
        }

        public async Task SaveCoordinatesAsync(double x, double y)
        {
            var template = await _context.OrdonnanceTemplates.FirstOrDefaultAsync() ?? new OrdonnanceTemplate();

            template.TemplateX = x;
            template.TemplateY = y;

            if (template.Id == 0)
            {
                _context.OrdonnanceTemplates.Add(template);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Point> GetStartingCoordinatesAsync()
        {
            var template = await _context.OrdonnanceTemplates.FirstOrDefaultAsync();
            if (template != null)
            {
                return new Point(template.TemplateX, template.TemplateY);
            }
            return new Point(50, 50); // Default coordinates
        }

        public async Task<string> SaveTemplateImageAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new ArgumentException("Invalid file path.", nameof(filePath));

            var templateFolder = Path.Combine(_appDataRoot, "OrdonanceTemplate");
            if (!Directory.Exists(templateFolder))
            {
                Directory.CreateDirectory(templateFolder);
            }

            string extension = Path.GetExtension(filePath);
            string newFileName = $"template_background{extension}";
            string destinationPath = Path.Combine(templateFolder, newFileName);

            File.Copy(filePath, destinationPath, overwrite: true);

            var template = await _context.OrdonnanceTemplates.FirstOrDefaultAsync() ?? new OrdonnanceTemplate();
            
            // Store the relative path instead of the hard absolute path to keep it portable
            template.TemplatePath = Path.Combine("OrdonanceTemplate", newFileName);

            if (template.Id == 0)
            {
                _context.OrdonnanceTemplates.Add(template);
            }

            await _context.SaveChangesAsync();
            return destinationPath;
        }

        public async Task<string> GetTemplatePathAsync()
        {
            var template = await _context.OrdonnanceTemplates.FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(template?.TemplatePath))
                return string.Empty;

            return Path.Combine(_appDataRoot, template.TemplatePath);
        }

        public FixedDocument CreatePrintDocument(IEnumerable<string> medicaments, Point startPoint, bool isPreview = false)
        {
            var fixedDoc = new FixedDocument();
            fixedDoc.DocumentPaginator.PageSize = new Size(793, 1122); // A4

            FixedPage fixedPage = new FixedPage { Width = 793, Height = 1122 };

            // If it's a preview, show the background image
            if (isPreview)
            {
                string path = GetTemplatePathAsync().Result; // Simple way for this context
                if (File.Exists(path))
                {
                    fixedPage.Children.Add(new Image
                    {
                        Source = new BitmapImage(new Uri(path)),
                        Width = 793,
                        Height = 1122,
                        Opacity = 0.5 // Make it faint so text is clear
                    });
                }
            }

            // Add Medications
            StackPanel list = new StackPanel();
            foreach (var med in medicaments)
            {
                list.Children.Add(new TextBlock { Text = med, FontSize = 16, Margin = new Thickness(0, 0, 0, 10) });
            }

            FixedPage.SetLeft(list, startPoint.X);
            FixedPage.SetTop(list, startPoint.Y);
            fixedPage.Children.Add(list);

            PageContent pc = new PageContent();
            ((IAddChild)pc).AddChild(fixedPage);
            fixedDoc.Pages.Add(pc);

            return fixedDoc;
        }
    }
}
