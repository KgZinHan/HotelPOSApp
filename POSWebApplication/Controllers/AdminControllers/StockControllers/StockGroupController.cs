﻿using Hotel_Core_MVC_V1.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSWebApplication.Data;
using POSWebApplication.Models;

namespace POSWebApplication.Controllers.AdminControllers.StockControllers
{
    [Authorize]
    public class StockGroupController : Controller
    {
        private readonly POSWebAppDbContext _dbContext;

        public StockGroupController(POSWebAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        #region // Main methods //
        public async Task<IActionResult> Index()
        {
            SetLayOutData();

            if (TempData["info message"] != null)
            {
                ViewBag.InfoMessage = TempData["info message"];
            }

            if (TempData["warning message"] != null)
            {
                ViewBag.WarningMessage = TempData["warning message"];
            }

            var groups = await _dbContext.ms_stockgroup.ToListAsync();

            var groupModelList = new StockGroupModelList()
            {
                StockGroups = groups
            };

            return View(groupModelList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockGroup stockGroup)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Add(stockGroup);
                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Stock Group is successfully created.";
            }
            else
            {
                TempData["warning message"] = "Required fields must be filled";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StockGroup stockGroup)
        {
            if (ModelState.IsValid)
            {
                _dbContext.ms_stockgroup.Update(stockGroup);
                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Stock Group is successfully updated!";
            }
            else
            {
                TempData["warning message"] = "Required fields must be filled.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(StockGroup stockGroup)
        {
            var dbStockGroup = await _dbContext.ms_stockgroup.FindAsync(stockGroup.StkGrpId);
            if (dbStockGroup != null)
            {
                _dbContext.ms_stockgroup.Remove(dbStockGroup);
            }

            await _dbContext.SaveChangesAsync();
            TempData["info message"] = "Stock Group is successfully deleted.";
            return RedirectToAction(nameof(Index));
        }

        #endregion


        #region // Global methods (Important!) //
        protected void SetLayOutData()
        {
            var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
            var user = _dbContext.pos_user.FirstOrDefault(u => u.UserCde == userCde);

            if (user != null)
            {
                ViewData["Username"] = user.UserNme;

                var accLevel = _dbContext.ms_usermenuaccess.FirstOrDefault(u => u.MnuGrpId == user.MnuGrpId)?.AccLevel;
                ViewData["User Role"] = accLevel.HasValue ? $"accessLvl{accLevel}" : null;

                var POS = _dbContext.ms_userpos.FirstOrDefault(pos => pos.UserId == user.UserId);

                var bizDte = _dbContext.ms_hotelinfo
                    .Where(info => info.Cmpyid == CommonItems.DefaultValues.CmpyId)
                    .Select(info => info.Hoteldte)
                    .FirstOrDefault() ?? new DateTime(1990, 01, 01);

                ViewData["Business Date"] = bizDte.ToString("dd MMM yyyy");
            }
        }

        #endregion


        #region // Partial View methods //

        public async Task<IActionResult> AddStockGroupPartial()
        {
            var groups = await _dbContext.ms_stockgroup.ToListAsync();

            var groupModelList = new StockGroupModelList()
            {
                StockGroups = groups
            };

            return PartialView("_AddStockGroupPartial", groupModelList);
        }

        public async Task<IActionResult> EditStockGroupPartial(short stkGrpId)
        {
            var group = await _dbContext.ms_stockgroup.FindAsync(stkGrpId);

            var groups = await _dbContext.ms_stockgroup.ToListAsync();

            var groupModelList = new StockGroupModelList()
            {
                StockGroups = groups,
                StockGroup = group
            };

            return PartialView("_EditStockGroupPartial", groupModelList);
        }

        public async Task<IActionResult> DeleteStockGroupPartial(short stkGrpId)
        {
            var group = await _dbContext.ms_stockgroup.FindAsync(stkGrpId);

            var groups = await _dbContext.ms_stockgroup.ToListAsync();

            var groupModelList = new StockGroupModelList()
            {
                StockGroups = groups,
                StockGroup = group
            };

            return PartialView("_DeleteStockGroupPartial", groupModelList);
        }

        #endregion

    }
}
