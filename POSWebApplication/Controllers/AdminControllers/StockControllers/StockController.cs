﻿using Hotel_Core_MVC_V1.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSWebApplication.Data;
using POSWebApplication.Models;
using SixLabors.ImageSharp;

namespace POSWebApplication.Controllers.AdminControllers.StockControllers
{
    [Authorize]
    public class StockController : Controller
    {
        private readonly POSWebAppDbContext _dbContext;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public StockController(POSWebAppDbContext dbContext, IWebHostEnvironment hostingEnvironment)
        {
            _dbContext = dbContext;
            _hostingEnvironment = hostingEnvironment;
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

            try
            {
                var stockList = await _dbContext.ms_stock.ToListAsync();
                var stockUOMs = await _dbContext.ms_stockuom.ToListAsync();

                var stockModelList = new StockModelList()
                {
                    StockList = stockList,
                    StockUOMs = stockUOMs
                };

                return View(stockModelList);
            }
            catch (Exception ex)
            {
                TempData["alert message"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Stock stock)
        {
            if (!ModelState.IsValid)
            {
                TempData["warning message"] = "Required fields must be filled";
                return RedirectToAction(nameof(Index));
            }

            if (await _dbContext.ms_stock.AnyAsync(stk => stk.ItemId == stock.ItemId))
            {
                TempData["warning message"] = "Item ID already exists. Please enter a different one.";
                return RedirectToAction(nameof(Index));
            }

            try
            {

                if (stock.ImageFile != null)
                {
                    if (stock.ImageFile.Length > 0 && stock.ImageFile.Length <= Stock.MaxImageSize)
                    {
                        using var image = Image.Load(stock.ImageFile.OpenReadStream()); // to change image file to varBinary

                        using var memoryStream = new MemoryStream();
                        stock.ImageFile.CopyTo(memoryStream);
                        stock.Image = memoryStream.ToArray();
                    }
                    else
                    {
                        TempData["warning message"] = "Image size needs to be less than 500KB.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else //adding default image
                {
                    string defaultImagePath = Path.Combine(_hostingEnvironment.WebRootPath, "images", "default.jpg");
                    var defaultImageBytes = System.IO.File.ReadAllBytes(defaultImagePath);
                    if (defaultImageBytes != null && defaultImageBytes.Length > 0)
                    {
                        stock.Image = defaultImageBytes;
                    }
                }
            }
            catch (Exception)
            {
                TempData["warning message"] = "Uploaded file is not an image.";
                return RedirectToAction(nameof(Index));
            }

            var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
            stock.UserId = _dbContext.pos_user.FirstOrDefault(u => u.UserCde == userCde)?.UserId;
            stock.CreateDtetime = DateTime.Now;
            _dbContext.ms_stock.Add(stock);
            TempData["info message"] = "Stock is successfully created.";

            var stockUOM = new StockUOM()
            {
                ItemId = stock.ItemId,
                UOMCde = stock.BaseUnit,
                UOMRate = 1,
                UnitCost = stock.UnitCost,
                SellingPrice = stock.SellingPrice
            };

            _dbContext.ms_stockuom.Add(stockUOM);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Stock stock)
        {
            if (ModelState.IsValid)
            {
                var UserCde = HttpContext.User.Claims.First()?.Value;
                stock.UserId = _dbContext.pos_user.FirstOrDefault(u => u.UserCde == UserCde)?.UserId;

                try
                {

                    if (stock.ImageFile != null)
                    {
                        if (stock.ImageFile.Length > 0 && stock.ImageFile.Length <= Stock.MaxImageSize)
                        {
                            using var image = Image.Load(stock.ImageFile.OpenReadStream()); // to change image file to varBinary

                            using var memoryStream = new MemoryStream();
                            stock.ImageFile.CopyTo(memoryStream);
                            stock.Image = memoryStream.ToArray();
                        }
                        else
                        {
                            TempData["warning message"] = "Image size needs to be less than 500KB.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                catch (Exception)
                {
                    TempData["warning message"] = "Uploaded file is not an image.";
                    return RedirectToAction(nameof(Index));
                }

                _dbContext.ms_stock.Update(stock);

                var thisStockUOM = await _dbContext.ms_stockuom.FirstOrDefaultAsync(uom => uom.UOMCde == stock.BaseUnit && uom.ItemId == stock.ItemId);
                if (thisStockUOM != null)
                {
                    thisStockUOM.UnitCost = stock.UnitCost;
                    thisStockUOM.SellingPrice = stock.SellingPrice;
                    _dbContext.ms_stockuom.Update(thisStockUOM);
                }

                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Stock is successfully updated!";
            }
            else
            {
                TempData["warning message"] = "Required fields must be filled.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Stock stock)
        {
            var dbStock = await _dbContext.ms_stock.FindAsync(stock.ItemId);

            if (dbStock != null)
            {
                _dbContext.ms_stock.Remove(dbStock);
            }

            var thisStockUOMs = await _dbContext.ms_stockuom.Where(u => u.ItemId == stock.ItemId).ToListAsync();
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == stock.ItemId).ToListAsync();

            if (thisStockUOMs != null)
            {
                foreach (var uom in thisStockUOMs)
                {
                    _dbContext.ms_stockuom.Remove(uom);
                }
            }

            if (thisStockBOMs != null)
            {
                foreach (var bom in thisStockBOMs)
                {
                    _dbContext.ms_stockbom.Remove(bom);
                }
            }

            await _dbContext.SaveChangesAsync();
            TempData["info message"] = "Stock is successfully deleted.";
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

        public async Task<IActionResult> AddStockPartial()
        {
            var stockList = await _dbContext.ms_stock.ToListAsync();
            var stockCategories = await _dbContext.ms_stockcategory.ToListAsync();
            var stockGroups = await _dbContext.ms_stockgroup.ToListAsync();

            var stockModelList = new StockModelList
            {
                StockList = stockList,
                StockCategories = stockCategories,
                StockGroups = stockGroups
            };

            return PartialView("_AddStockPartial", stockModelList);
        }

        public async Task<IActionResult> EditStockPartial(string itemId)
        {
            var stock = await _dbContext.ms_stock.FindAsync(itemId);

            if (stock != null)
            {
                stock.UserCde = _dbContext.pos_user.Find(stock.UserId)?.UserCde;
                stock.Base64Image = stock.Image != null ? Convert.ToBase64String(stock.Image) : "";
            }


            var stockList = await _dbContext.ms_stock.ToListAsync();
            var stockUOMList = await _dbContext.ms_stockuom.Where(u => u.ItemId.Equals(itemId)).ToListAsync();
            var stockCategories = await _dbContext.ms_stockcategory.ToListAsync();
            var stockGroups = await _dbContext.ms_stockgroup.ToListAsync();

            var stockModelList = new StockModelList
            {
                Stock = stock,
                StockList = stockList,
                StockUOMs = stockUOMList,
                StockCategories = stockCategories,
                StockGroups = stockGroups
            };

            return PartialView("_EditStockPartial", stockModelList);
        }

        public async Task<IActionResult> DeleteStockPartial(string itemId)
        {
            var stock = await _dbContext.ms_stock.FindAsync(itemId);

            if (stock != null)
            {
                stock.UserCde = _dbContext.pos_user.Find(stock.UserId)?.UserCde;
            }

            var stockList = await _dbContext.ms_stock.ToListAsync();

            var stockModelList = new StockModelList
            {
                Stock = stock,
                StockList = stockList
            };

            return PartialView("_DeleteStockPartial", stockModelList);
        }

        #endregion

    }
}
