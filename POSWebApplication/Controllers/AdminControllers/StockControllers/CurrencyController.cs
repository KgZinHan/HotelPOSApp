﻿using Hotel_Core_MVC_V1.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSWebApplication.Data;
using POSWebApplication.Models;

namespace POSWebApplication.Controllers.AdminControllers.StockControllers
{
    [Authorize]
    public class CurrencyController : Controller
    {
        private readonly POSWebAppDbContext _dbContext;

        public CurrencyController(POSWebAppDbContext dbContext)
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

            var currencies = await _dbContext.ms_currency.ToListAsync();

            var currencyModelList = new CurrencyModelList()
            {
                Currencies = currencies
            };

            return View(currencyModelList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Currency currency)
        {
            if (ModelState.IsValid)
            {
                var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
                var userId = _dbContext.pos_user.Where(u => u.UserCde == userCde).Select(u => u.UserId).FirstOrDefault();
                currency.UserId = userId;
                currency.RevDteTime = DateTime.Now;
                _dbContext.Add(currency);
                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Currency is successfully created.";
            }
            else
            {
                TempData["warning message"] = "Required fields must be filled";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Currency currency)
        {
            if (ModelState.IsValid)
            {
                var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
                var userId = _dbContext.pos_user.Where(u => u.UserCde == userCde).Select(u => u.UserId).FirstOrDefault();
                currency.UserId = userId;
                currency.RevDteTime = DateTime.Now;
                _dbContext.ms_currency.Update(currency);
                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Currency is successfully updated!";
            }
            else
            {
                TempData["warning message"] = "Required fields must be filled.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Currency currency)
        {
            var dbCurrency = await _dbContext.ms_currency.FindAsync(currency.CurrId);
            if (dbCurrency != null)
            {
                _dbContext.ms_currency.Remove(dbCurrency);
            }

            await _dbContext.SaveChangesAsync();
            TempData["info message"] = "Currency is successfully deleted.";
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

        public async Task<IActionResult> AddCurrencyPartial()
        {
            var currencies = await _dbContext.ms_currency.ToListAsync();

            var currencyModelList = new CurrencyModelList()
            {
                Currencies = currencies
            };

            return PartialView("_AddCurrencyPartial", currencyModelList);
        }

        public async Task<IActionResult> EditCurrencyPartial(int currId)
        {
            var currency = await _dbContext.ms_currency.FindAsync(currId);
            var currencies = await _dbContext.ms_currency.ToListAsync();

            var currencyModelList = new CurrencyModelList()
            {
                Currencies = currencies,
                Currency = currency
            };

            return PartialView("_EditCurrencyPartial", currencyModelList);
        }

        public async Task<IActionResult> DeleteCurrencyPartial(int currId)
        {
            var currency = await _dbContext.ms_currency.FindAsync(currId);
            var currencies = await _dbContext.ms_currency.ToListAsync();

            var currencyModelList = new CurrencyModelList()
            {
                Currencies = currencies,
                Currency = currency
            };

            return PartialView("_DeleteCurrencyPartial", currencyModelList);
        }

        #endregion
    }
}
