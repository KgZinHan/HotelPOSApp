﻿using Hotel_Core_MVC_V1.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using POSWebApplication.Data;
using POSWebApplication.Models;

namespace POSWebApplication.Controllers.AdminControllers.UsersControllers
{
    [Authorize]
    public class MenuAccessController : Controller
    {
        private readonly POSWebAppDbContext _dbContext;

        public MenuAccessController(POSWebAppDbContext dbContext)
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
            try
            {
                var menuAccessList = await _dbContext.ms_usermenuaccess.ToListAsync();
                return View(menuAccessList);
            }
            catch (Exception ex)
            {
                TempData["alert message"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Create()
        {
            SetLayOutData();

            if (ViewData["User Role"]?.ToString() == "accessLvl2")
            {
                var modelList = new MenuAccessModelList()
                {
                    MenuAccess = new MenuAccess(),
                    UserMenuGroupList = _dbContext.ms_usermenugrp.ToList()
                };

                return View(modelList);
            }

            return RedirectToAction("Index");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MnuGrpId,AccLevel")] MenuAccess menuAccess)
        {
            SetLayOutData();

            var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
            var user = await _dbContext.pos_user.FirstOrDefaultAsync(u => u.UserCde == userCde);

            if (ModelState.IsValid && user != null)
            {
                menuAccess.RevDtetime = DateTime.Now;
                menuAccess.ByUserID = user.UserId;
                menuAccess.BtnNme = "access";

                _dbContext.Add(menuAccess);
                await _dbContext.SaveChangesAsync();
                TempData["info message"] = "Menu Access is successfully created!";
                return RedirectToAction(nameof(Index));
            }

            var menuAccessModelList = new MenuAccessModelList()
            {
                MenuAccess = menuAccess,
                UserMenuGroupList = await _dbContext.ms_usermenugrp.ToListAsync()
            };

            return View(menuAccess);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            SetLayOutData();

            if (ViewData["User Role"]?.ToString() == "accessLvl2")
            {
                if (id == null || _dbContext.ms_usermenuaccess == null)
                {
                    return NotFound();
                }

                var menuAccess = await _dbContext.ms_usermenuaccess.FindAsync(id);
                if (menuAccess == null)
                {
                    return NotFound();
                }

                var menuAccessModelList = new MenuAccessModelList()
                {
                    MenuAccess = menuAccess,
                    UserMenuGroupList = await _dbContext.ms_usermenugrp.ToListAsync()
                };

                return View(menuAccessModelList);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AccessId,MnuGrpId,AccLevel,BtnNme,RevDtetime,ByUserID")] MenuAccess menuAccess)
        {
            SetLayOutData();

            if (id != menuAccess.AccessId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userCde = HttpContext.User.Claims.FirstOrDefault()?.Value;
                var userId = await _dbContext.pos_user.Where(u => u.UserCde == userCde).Select(u => u.UserId).FirstOrDefaultAsync();
                try
                {
                    menuAccess.RevDtetime = DateTime.Now;
                    menuAccess.ByUserID = userId;
                    _dbContext.Update(menuAccess);
                    await _dbContext.SaveChangesAsync();
                    TempData["info message"] = "Menu Access is successfully updated!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuAccessExists(menuAccess.AccessId))
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

            var menuAccessModelList = new MenuAccessModelList()
            {
                MenuAccess = menuAccess,
                UserMenuGroupList = await _dbContext.ms_usermenugrp.ToListAsync()
            };

            return View(menuAccessModelList);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            SetLayOutData();

            if (ViewData["User Role"]?.ToString() == "accessLvl2")
            {
                if (id == null || _dbContext.ms_usermenuaccess == null)
                {
                    return NotFound();
                }

                var menuAccess = await _dbContext.ms_usermenuaccess
                    .FirstOrDefaultAsync(m => m.AccessId == id);
                if (menuAccess == null)
                {
                    return NotFound();
                }

                var menuAccessModelList = new MenuAccessModelList()
                {
                    MenuAccess = menuAccess,
                    UserMenuGroupList = await _dbContext.ms_usermenugrp.ToListAsync()
                };
                return View(menuAccessModelList);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(MenuAccess menuAccess)
        {
            SetLayOutData();

            var dbUser = await _dbContext.ms_usermenuaccess.FindAsync(menuAccess.AccessId);

            if (dbUser != null)
            {
                _dbContext.ms_usermenuaccess.Remove(dbUser);
            }

            await _dbContext.SaveChangesAsync();
            TempData["info message"] = "Menu Access is successfully deleted!";
            return RedirectToAction("Index");

        }

        private bool MenuAccessExists(int id)
        {
            return (_dbContext.ms_usermenuaccess?.Any(e => e.AccessId == id)).GetValueOrDefault();
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
    }
}
