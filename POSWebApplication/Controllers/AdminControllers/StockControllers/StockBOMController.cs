﻿using Hotel_Core_MVC_V1.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSWebApplication.Data;
using POSWebApplication.Models;

namespace POSWebApplication.Controllers.AdminControllers.StockControllers
{
    [Authorize]
    public class StockBOMController : Controller
    {
        private readonly POSWebAppDbContext _dbContext;

        public StockBOMController(POSWebAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        #region // Main methods //

        public async Task<IActionResult> Index()
        {
            SetLayOutData();

            var stocks = await _dbContext.ms_stock.Where(u => u.FinishGoodFlg == true).ToListAsync();

            var bOMModelList = new StockBOMModelList()
            {
                Stocks = stocks
            };

            return View(bOMModelList);
        }

        public async Task<IActionResult> Create(StockBOM stockBOM)
        {
            if (ModelState.IsValid)
            {
                var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;

                var userId = _dbContext.pos_user
                    .Where(u => u.UserCde == userCde)
                    .Select(u => u.UserId)
                    .FirstOrDefault();

                stockBOM.RevDteTime = DateTime.Now;
                stockBOM.UserId = userId;

                await _dbContext.AddAsync(stockBOM);
                await _dbContext.SaveChangesAsync();
            }

            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == stockBOM.BOMId).OrderBy(u => u.OrdId).ToListAsync();

            foreach (var bom in thisStockBOMs)
            {
                bom.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                ThisStockBOMs = thisStockBOMs,
                StockBOM = new StockBOM()
                {
                    BOMId = stockBOM.BOMId
                }
            };

            return PartialView("_StockBOMPartial", bOMModelList);
        }

        public async Task<IActionResult> Edit(StockBOM stockBOM)
        {
            if (ModelState.IsValid)
            {
                var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;

                var userId = _dbContext.pos_user
                    .Where(u => u.UserCde == userCde)
                    .Select(u => u.UserId)
                    .FirstOrDefault();

                stockBOM.RevDteTime = DateTime.Now;
                stockBOM.UserId = userId;

                _dbContext.ms_stockbom.Update(stockBOM);
                await _dbContext.SaveChangesAsync();
            }

            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == stockBOM.BOMId).OrderBy(u => u.OrdId).ToListAsync();

            foreach (var bom in thisStockBOMs)
            {
                bom.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                ThisStockBOMs = thisStockBOMs,
                StockBOM = new StockBOM()
                {
                    BOMId = stockBOM.BOMId
                }
            };

            return PartialView("_StockBOMPartial", bOMModelList);
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

        public async Task<IActionResult> StockBOMPartial(string bomId)
        {
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();

            foreach (var stockBOM in thisStockBOMs)
            {
                stockBOM.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                ThisStockBOMs = thisStockBOMs,
                StockBOM = new StockBOM()
                {
                    BOMId = bomId
                }
            };

            return PartialView("_StockBOMPartial", bOMModelList);
        }

        public async Task<IActionResult> AddStockBOMPartial(string bomId)
        {
            var stocks = await _dbContext.ms_stock
                .Select(stock => new StockBOM
                {
                    ItemId = stock.ItemId,
                    BaseUnit = stock.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'I'
                })
                .ToListAsync();

            var serviceItems = await _dbContext.ms_serviceitem
                .Select(serviceItem => new StockBOM
                {
                    ItemId = serviceItem.SrvcItemId,
                    BaseUnit = serviceItem.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'S'
                })
                .ToListAsync();

            var ordId = await _dbContext.ms_stockbom
                .Where(u => u.BOMId == bomId)
                .Select(u => u.OrdId)
                .OrderBy(u => u)
                .LastOrDefaultAsync();

            var unionStockBOMs = stocks.Union(serviceItems);
            var firstItemId = unionStockBOMs.FirstOrDefault()?.ItemId;
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();
            var thisStockUOMs = await _dbContext.ms_stockuom.Where(u => u.ItemId == firstItemId).ToListAsync();

            foreach (var stockBOM in thisStockBOMs)
            {
                stockBOM.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                UnionStockBOMs = unionStockBOMs,
                ThisStockBOMs = thisStockBOMs,
                ThisStockUOMs = thisStockUOMs,
                StockBOM = new StockBOM()
                {
                    OrdId = (short)(ordId + 1),
                    BOMId = bomId,
                    UOMRate = 1
                }
            };

            return PartialView("_AddStockBOMPartial", bOMModelList);
        }

        public async Task<IActionResult> EditStockBOMPartial(string bomId, int stkBOMId)
        {
            var thisStockUOMs = new List<StockUOM>();
            Boolean flag = false;

            var stocks = _dbContext.ms_stock
                .Select(stock => new StockBOM
                {
                    ItemId = stock.ItemId,
                    BaseUnit = stock.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'I'
                });

            var serviceItems = _dbContext.ms_serviceitem
                .Select(serviceItem => new StockBOM
                {
                    ItemId = serviceItem.SrvcItemId,
                    BaseUnit = serviceItem.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'S'
                });

            var ordId = _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).Select(u => u.OrdId).OrderBy(u => u).LastOrDefault();

            var unionStockBOMs = stocks.Union(serviceItems);
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();
            var thisStockBOM = await _dbContext.ms_stockbom.FindAsync(stkBOMId);
            var itemId = thisStockBOM?.ItemId;

            if (itemId != null)
            {
                thisStockUOMs = await _dbContext.ms_stockuom.Where(u => u.ItemId == itemId).ToListAsync();
                if (thisStockUOMs.Count <= 0)
                {
                    flag = true;
                }
            }

            foreach (var stockBOM in thisStockBOMs)
            {
                stockBOM.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList
            {
                UnionStockBOMs = unionStockBOMs,
                ThisStockBOMs = thisStockBOMs,
                ThisStockUOMs = thisStockUOMs,
                StockBOM = thisStockBOM,
                BaseUnitFlg = flag
            };

            return PartialView("_EditStockBOMPartial", bOMModelList);
        }

        public async Task<IActionResult> DeleteStockBOMPartial(string bomId, int stkbomId)
        {
            var dbStkBOM = await _dbContext.ms_stockbom.FindAsync(stkbomId);

            if (dbStkBOM != null)
            {
                _dbContext.ms_stockbom.Remove(dbStkBOM);
                await _dbContext.SaveChangesAsync();
            }

            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();

            foreach (var stockBOM in thisStockBOMs)
            {
                stockBOM.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                ThisStockBOMs = thisStockBOMs,
                StockBOM = new StockBOM()
                {
                    BOMId = bomId
                }
            };

            return PartialView("_StockBOMPartial", bOMModelList);
        }

        public async Task<IActionResult> SelectItemPartial(string bomId, string itemId)
        {
            Boolean flag = false;

            var stocks = await _dbContext.ms_stock
                .Select(stock => new StockBOM
                {
                    ItemId = stock.ItemId,
                    BaseUnit = stock.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'I'
                })
                .ToListAsync();

            var serviceItems = await _dbContext.ms_serviceitem
                .Select(serviceItem => new StockBOM
                {
                    ItemId = serviceItem.SrvcItemId,
                    BaseUnit = serviceItem.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'S'
                })
                .ToListAsync();

            var ordId = await _dbContext.ms_stockbom
                .Where(u => u.BOMId == bomId)
                .Select(u => u.OrdId)
                .OrderBy(u => u)
                .LastOrDefaultAsync();

            var unionStockBOMs = stocks.Union(serviceItems);
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();
            var thisStockUOMs = await _dbContext.ms_stockuom.Where(u => u.ItemId == itemId).ToListAsync();

            var thisStockBOM = unionStockBOMs.Where(u => u.ItemId == itemId).Select(stock => new StockBOM
            {
                BOMId = bomId,
                OrdId = (short)(ordId + 1),// to get next ord
                ItemId = stock.ItemId,
                BaseUnit = stock.BaseUnit,
                UOMRate = stock.UOMRate,
                Qty = stock.Qty,
                StockFlg = stock.StockFlg
            }).FirstOrDefault();

            if (thisStockBOM?.StockFlg == 'S')
            {
                flag = true;
            }

            foreach (var stockBOM in thisStockBOMs)
            {
                stockBOM.UOMRate = 1;
            }

            var bOMModelList = new StockBOMModelList()
            {
                UnionStockBOMs = unionStockBOMs,
                ThisStockBOMs = thisStockBOMs,
                ThisStockUOMs = thisStockUOMs,
                StockBOM = thisStockBOM,
                BaseUnitFlg = flag
            };

            return PartialView("_AddStockBOMPartial", bOMModelList);
        }

        #endregion


        #region // Stock BOM Items methods //

        public async Task<IEnumerable<StockBOM>> GetStockBOMList(string bomId)
        {
            var thisStockBOMs = await _dbContext.ms_stockbom.Where(u => u.BOMId == bomId).OrderBy(u => u.OrdId).ToListAsync();

            return thisStockBOMs;
        }

        public IEnumerable<StockBOM> GetStocks()
        {
            var stocks = _dbContext.ms_stock
                .Select(stock => new StockBOM
                {
                    ItemId = stock.ItemId,
                    BaseUnit = stock.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'I'
                })
                .ToList();

            var serviceItems = _dbContext.ms_serviceitem
                .Select(serviceItem => new StockBOM
                {
                    ItemId = serviceItem.SrvcItemId,
                    BaseUnit = serviceItem.BaseUnit,
                    UOMRate = 1,
                    StockFlg = 'S'
                })
                .ToList();

            var unionStocks = stocks.Union(serviceItems).Select(sItem => new StockBOM
            {
                ItemId = sItem.ItemId,
                BaseUnit = sItem.BaseUnit,
                UOMRate = 1,
                StockFlg = sItem.StockFlg
            });


            return unionStocks;
        }

        #endregion

    }
}
