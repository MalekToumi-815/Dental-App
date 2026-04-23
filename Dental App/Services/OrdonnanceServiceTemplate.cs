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
using System.Linq;

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
        FixedDocument CreatePrintDocument(IEnumerable<string> medicaments, Point startPoint, bool isPreview = false);
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
            var doc = new FixedDocument();

            try
            {
                // Try to load background image if available
                string templateFullPath = null;
                var template = _context.OrdonnanceTemplates.FirstOrDefault();
                if (template != null && !string.IsNullOrWhiteSpace(template.TemplatePath))
                {
                    templateFullPath = Path.Combine(_appDataRoot, template.TemplatePath);
                    if (!File.Exists(templateFullPath))
                        templateFullPath = null;
                }

                BitmapImage background = null;
                double pageWidth = 800;
                double pageHeight = 1100;

                if (!string.IsNullOrEmpty(templateFullPath))
                {
                    background = new BitmapImage();
                    background.BeginInit();
                    background.UriSource = new Uri(templateFullPath, UriKind.Absolute);
                    background.CacheOption = BitmapCacheOption.OnLoad;
                    background.EndInit();
                    background.Freeze();

                    pageWidth = background.PixelWidth;
                    pageHeight = background.PixelHeight;
                }

                // Build a single page document. You can extend to multiple pages if needed.
                var pageContent = new PageContent();
                var fixedPage = new FixedPage
                {
                    Width = pageWidth,
                    Height = pageHeight,
                    Background = Brushes.White
                };

                if (background != null && isPreview)
                {
                    var image = new Image
                    {
                        Source = background,
                        Width = pageWidth,
                        Height = pageHeight,
                        Stretch = Stretch.Fill
                    };
                    FixedPage.SetLeft(image, 0);
                    FixedPage.SetTop(image, 0);
                    fixedPage.Children.Add(image);
                }

                // Compose text block for the medicaments
                var textBlock = new TextBlock
                {
                    FontSize = 14,
                    Foreground = Brushes.Black,
                    TextWrapping = TextWrapping.Wrap
                };

                var lines = medicaments?.ToArray() ?? Array.Empty<string>();
                textBlock.Text = string.Join(Environment.NewLine, lines);

                FixedPage.SetLeft(textBlock, startPoint.X);
                FixedPage.SetTop(textBlock, startPoint.Y);

                fixedPage.Children.Add(textBlock);

                ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
                doc.Pages.Add(pageContent);
            }
            catch
            {
                // On any error, return an empty FixedDocument instead of crashing the caller
            }

            return doc;
        }
    }
}
