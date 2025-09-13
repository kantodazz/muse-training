using Elmah;
using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using IFMIS.Libraries;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class ItemCardsController : Controller
    {

        private IFMISTZDbContext db = new IFMISTZDbContext();
        public ActionResult CreateItemCard()
        {
            InventoryItemCard inventoryItemCard = new InventoryItemCard();
            inventoryItemCard.UOMList = new SelectList(db.UOMs, "UomName", "UomName");
            return View(inventoryItemCard);
        }
        public ActionResult ItemCardsList()
        {
            InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
            var itemCardList = db.InventoryItemCards.Where(a => a.InstitutionCode == userPaystation.InstitutionCode&& a.CardStatus=="Active").OrderByDescending(a=>a.InventoryItemCardId).ToList();
            return View(itemCardList);
        }

        public JsonResult SaveItemCard(InventoryItemCard itemCard)
        {
            string response = null;
            try
            {
                InstitutionSubLevel userPaystation = ServiceManager.GetUserPayStation(db, User.Identity.GetUserId());
                itemCard.InstitutionCode = userPaystation.InstitutionCode;
                itemCard.AvailableQuantity = 0;
                itemCard.AvailableQtyForSale = 0;
                itemCard.CardStatus = "Active";
                itemCard.CreatedBy = User.Identity.Name;
                itemCard.CreatedAt = DateTime.Now;
                db.InventoryItemCards.Add(itemCard);
                db.SaveChanges();
                itemCard.ItemCard = generateItemCard(itemCard);
                db.SaveChanges();
                response = "Success";
                var result_data = new {response, ItemCard = itemCard.ItemCard};
                return Json(result_data, JsonRequestBehavior.AllowGet);

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";

            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public ActionResult EditItemCard(int? id)
        {
            InventoryItemCard inventoryItemCard = db.InventoryItemCards.Find(id);
            inventoryItemCard.UOMList = new SelectList(db.UOMs, "UomName", "UomName");
            return View(inventoryItemCard);
        }
        public JsonResult SaveEditItemCard(InventoryItemCard itemCard)
        {
            string response = null;
            try
            {
                InventoryItemCard inventoryItemCard = db.InventoryItemCards.Find(itemCard.InventoryItemCardId);
                inventoryItemCard.ItemDescription = itemCard.ItemDescription;
                inventoryItemCard.OrderLevel = itemCard.OrderLevel;
                inventoryItemCard.OrderLevelForSale = itemCard.OrderLevelForSale;
                inventoryItemCard.UOM = itemCard.UOM;
                db.Entry(inventoryItemCard).State = EntityState.Modified;
                db.SaveChanges();
                response = "Success";
            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";

            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }
        public string generateItemCard(InventoryItemCard inventoryItemCard)
        {
            string substringNumber = null;
            string ItemCard = null;
            var count = db.InventoryItemCards.Where(a => a.InstitutionCode == inventoryItemCard.InstitutionCode).Count() + 1;
            if (count < 10)
            {
                substringNumber = "000" + count;
            }
            else if (count < 100)
            {
                substringNumber = "00" + count;
            }
            else if (count < 1000)
            {
                substringNumber = "0" + count;
            }
            else
            {
                substringNumber = count.ToString();
            }
            ItemCard = inventoryItemCard.ItemDescription + "-" + inventoryItemCard.InstitutionCode + "-" + substringNumber;
            return ItemCard;
        }

        [Authorize(Roles = "Inventory Info Entry")]
        public JsonResult CancellItemCard(int? id)
        {
            string response = null;
            try
            {
                InventoryDetail inventoryDetail = db.InventoryDetails.Where(a => a.InventoryItemCardId == id && a.OverallStatus != "Cancelled").FirstOrDefault();
                if (inventoryDetail == null) { 
                InventoryItemCard inventoryItemCard = db.InventoryItemCards.Find(id);
                inventoryItemCard.CardStatus = "Cancelled";
                db.Entry(inventoryItemCard).State = EntityState.Modified;
                response = "Success";
                   }
                  else
                  {
                response = "Attached";
                   }

            }
            catch (Exception ex)
            {
                ErrorSignal.FromCurrentContext().Raise(ex);
                response = "DbException";
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}