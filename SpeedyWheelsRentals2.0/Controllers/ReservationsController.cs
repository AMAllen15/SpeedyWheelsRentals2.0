﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpeedyWheelsRentals.Models;
using SpeedyWheelsRentals2._0.Data;
using SpeedyWheelsRentals2._0.Services;

namespace SpeedyWheelsRentals2._0.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        private readonly ReportService _reportService;

        private readonly GenerateBillService _generateBillService;

        public ReservationsController(ApplicationDbContext context, ReportService reportService, GenerateBillService generateBillService)
        {
            _context = context;
           
            _reportService = reportService;
            _generateBillService = generateBillService;
        }

        // Generate the business report
        public async Task<IActionResult> ExportToPdf()
        {
            var reservations = await _context.Reservation.Include(r => r.Customer).Include(r=>r.Vehicle).ToListAsync();
            var tempFilePath = _reportService.GeneratePdfReportToFile(reservations);

            if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
            {
                return NotFound(); // Handle scenario where file generation failed or file doesn't exist
            }

            var fileContent = await System.IO.File.ReadAllBytesAsync(tempFilePath);
            System.IO.File.Delete(tempFilePath); // Delete the temp file after reading its content into memory

            return File(fileContent, "application/pdf", "ReservationsReport.pdf");
        }

        // Generate billing for each reservation
        public async Task<IActionResult> Billing(Guid? id)
        {
            if (id == null || _context.Reservation == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            var tempFilePath = _generateBillService.GeneratePdfBillToFile(reservation);

            if (string.IsNullOrEmpty(tempFilePath) || !System.IO.File.Exists(tempFilePath))
            {
                return NotFound(); // Handle scenario where file generation failed or file doesn't exist
            }

            var fileContent = await System.IO.File.ReadAllBytesAsync(tempFilePath);
            System.IO.File.Delete(tempFilePath); // Delete the temp file after reading its content into memory

            return File(fileContent, "application/pdf", "ReservationsReport.pdf");
            
            

        }


        // GET: Reservations
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Reservation.Include(r => r.Customer).Include(r => r.Vehicle);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> ShowSearchForm()
        {

            return View();
        }


        // POST: Rservation/ShowSearchResult
        public async Task<IActionResult> ShowSearchResult(String SearchPhrase)
        {
            var applicationDbContext = _context.Reservation.Include(r => r.Customer).Include(r => r.Vehicle);
            return View("index", await applicationDbContext.Where(r => r.Customer.Name.Contains(SearchPhrase)).ToListAsync());
            

        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Reservation == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservations/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "CustomerId");
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "VehicleId");
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ReservationStatus))
                                  .Cast<ReservationStatus>()
                                  .Select(e => new { Value = e.ToString(), Text = e.ToString() }),
                                  "Value", "Text");
            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("ReservationId,CustomerId,VehicleId,StartDate,EndDate,Status,ReservationCost")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                reservation.ReservationId = Guid.NewGuid();
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                var vehicle = _context.Vehicle.Include(v => v.Reservations)
                                        .FirstOrDefault(v => v.VehicleId == reservation.VehicleId);

                vehicle?.UpdateStatusBasedOnReservations();

                // Save changes again to update the vehicle status
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ReservationStatus))
                                  .Cast<ReservationStatus>()
                                  .Select(e => new { Value = e.ToString(), Text = e.ToString() }),
                                  "Value", "Text");
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "CustomerId", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "VehicleId", reservation.VehicleId);
            
            return View(reservation);
        }

        // GET: Reservations/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Reservation == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ReservationStatus))
                                 .Cast<ReservationStatus>()
                                 .Select(e => new { Value = e.ToString(), Text = e.ToString() }),
                                 "Value", "Text");
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "CustomerId", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "VehicleId", reservation.VehicleId);
            return View(reservation);
        }

        // POST: Reservations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id, [Bind("ReservationId,CustomerId,VehicleId,StartDate,EndDate,Status,ReservationCost")] Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();

                    var vehicle = _context.Vehicle.Include(v => v.Reservations)
                                        .FirstOrDefault(v => v.VehicleId == reservation.VehicleId);

                    vehicle?.UpdateStatusBasedOnReservations();

                    // Save changes again to update the vehicle status
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customer, "CustomerId", "CustomerId", reservation.CustomerId);
            ViewData["VehicleId"] = new SelectList(_context.Vehicle, "VehicleId", "VehicleId", reservation.VehicleId);
            return View(reservation);
        }

        // GET: Reservations/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Reservation == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservation
                .Include(r => r.Customer)
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Reservation == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Reservation'  is null.");
            }
            var reservation = await _context.Reservation.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservation.Remove(reservation);
            }
            
            await _context.SaveChangesAsync();
            var vehicle = _context.Vehicle.Include(v => v.Reservations)
                                        .FirstOrDefault(v => v.VehicleId == reservation.VehicleId);

            vehicle?.UpdateStatusBasedOnReservations();

            // Save changes again to update the vehicle status
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(Guid id)
        {
          return (_context.Reservation?.Any(e => e.ReservationId == id)).GetValueOrDefault();
        }
    }
}
