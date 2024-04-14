﻿using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SpeedyWheelsRentals.Models;
using System.Collections.Generic;
using System.IO;



namespace SpeedyWheelsRentals2._0.Services
{


    public class ReportService
    {

        public string GeneratePdfReportToFile(List<Reservation> reservations)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".pdf");
            using (var writer = new PdfWriter(tempFilePath))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
                    document.Add(new Paragraph("Reservations Report").SetTextAlignment(TextAlignment.CENTER));

                    Table table = new Table(UnitValue.CreatePercentArray(new float[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 })).UseAllAvailableWidth();
                    table.AddHeaderCell("Start Date");
                    table.AddHeaderCell("End Date");
                    table.AddHeaderCell("Status");
                    table.AddHeaderCell("Customer Name");
                    table.AddHeaderCell("Customer Email");
                    table.AddHeaderCell("Customer PhoneNumber");
                    table.AddHeaderCell("Car Make");
                    table.AddHeaderCell("Car Model");
                    table.AddHeaderCell("Car RegistrationNumber");
                    table.AddHeaderCell("Cost");

                    foreach (var reservation in reservations)
                    {
                        table.AddCell(reservation.StartDate.ToString());
                        table.AddCell(reservation.EndDate.ToString());
                        table.AddCell(reservation.Status.ToString());
                        table.AddCell(reservation.Customer?.Name ?? "N/A");
                        table.AddCell(reservation.Customer?.Email ?? "N/A");
                        table.AddCell(reservation.Customer?.PhoneNumber ?? "N/A");
                        table.AddCell(reservation.Vehicle?.Make ?? "N/A");
                        table.AddCell(reservation.Vehicle?.Model ?? "N/A");
                        table.AddCell(reservation.Vehicle?.RegistrationNumber ?? "N/A");
                        table.AddCell(reservation.ReservationCost.ToString("C"));
                    }

                    document.Add(table);
                }
            }
            return tempFilePath; // Return the path to the temporary file
        }




    }
}
