using IFMIS.Areas.IFMISTZ.Models;
using IFMIS.DAL;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace IFMIS.Areas.IFMISTZ.Controllers
{
    [Authorize]
    public class FinalAccountItemrangesController : Controller
    {
        private IFMISTZDbContext db = new IFMISTZDbContext();

        // GET: IFMISTZ/FinalAccountItemranges
        [HttpGet, Authorize(Roles = "Final Account Ranges Entry")]
        public ActionResult FinalAccount()
        {
            var finalAccountItemranges = db.FinalAccountItemranges
                .OrderByDescending(a => a.DateCreated)
                .Include(f => f.FaClassificationCode)
                .Include(f => f.FinalAccountItems);

            return View(finalAccountItemranges.ToList());
        }

        // GET: IFMISTZ/FinalAccountItemranges/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FinalAccountItemrange finalAccountItemrange = db.FinalAccountItemranges.Find(id);
            if (finalAccountItemrange == null)
            {
                return HttpNotFound();
            }
            return View(finalAccountItemrange);
        }

        // GET: IFMISTZ/FinalAccountItemranges/Create
        [HttpGet, Authorize(Roles = "Final Account Ranges Entry")]
        public ActionResult Create()
        {
            ViewBag.ClassificationId = new SelectList(db.FaClassificationCodes, "ClassificationId", "ClassificationCodeClassificationDesc");
            ViewBag.FinalAccountItemsId = new SelectList(db.FinalAccountItemss, "FinalAccountItemsId", "NoteNoItemDescription");
            return View();
        }


        //[HttpPost]
        [HttpPost, Authorize(Roles = "Final Account Ranges Entry")]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FinalAccountItemrangesId,ClassificationId,FinalAccountItemsId,NoteNo,ItemStart,ItemEnd,ActiveFlag,DateCreated,UserCreated,DateModified,UserModified")] FinalAccountItemrange finalAccountItemrange)
        {
            int count = 0;
            count = db.FinalAccountItemranges.Where(ab => ab.FinalAccountItemsId == finalAccountItemrange.FinalAccountItemsId && ab.ClassificationId == finalAccountItemrange.ClassificationId && ab.ItemStart == finalAccountItemrange.ItemStart).Count();
            if (count >= 1)
            {
                TempData["Success"] = "No";
                return RedirectToAction("Create");
            }


            if (ModelState.IsValid)
            {
                var noteno = db.FinalAccountItemss.Where(ac => ac.FinalAccountItemsId == finalAccountItemrange.FinalAccountItemsId).FirstOrDefault();
                finalAccountItemrange.NoteNo = noteno.NoteNo;
                finalAccountItemrange.UserCreated = User.Identity.Name;
                finalAccountItemrange.UserModified = User.Identity.Name;
                finalAccountItemrange.DateCreated = DateTime.Now;
                finalAccountItemrange.DateModified = DateTime.Now;


                db.FinalAccountItemranges.Add(finalAccountItemrange);
                db.SaveChanges();
                TempData["Success"] = "Success";
                return RedirectToAction("Create");
            }

            ViewBag.ClassificationId = new SelectList(db.FaClassificationCodes, "ClassificationId", "ClassificationCodeClassificationDesc", finalAccountItemrange.ClassificationId);
            ViewBag.FinalAccountItemsId = new SelectList(db.FinalAccountItemss, "FinalAccountItemsId", "NoteNoItemDescription", finalAccountItemrange.FinalAccountItemsId);
            return View(finalAccountItemrange);
        }

        // GET: IFMISTZ/FinalAccountItemranges/Edit/5
        [HttpGet, Authorize(Roles = "Final Account Ranges Entry")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FinalAccountItemrange finalAccountItemrange = db.FinalAccountItemranges.Find(id);
            if (finalAccountItemrange == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClassificationId = new SelectList(db.FaClassificationCodes, "ClassificationId", "ClassificationCodeClassificationDesc", finalAccountItemrange.ClassificationId);
            ViewBag.FinalAccountItemsId = new SelectList(db.FinalAccountItemss, "FinalAccountItemsId", "NoteNoItemDescription", finalAccountItemrange.FinalAccountItemsId);
            return View(finalAccountItemrange);
        }


        //[HttpPost]
        [HttpPost, Authorize(Roles = "Final Account Ranges Entry")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FinalAccountItemrangesId,ClassificationId,FinalAccountItemsId,NoteNo,ItemStart,ItemEnd,ActiveFlag,DateCreated,UserCreated,DateModified,UserModified")] FinalAccountItemrange finalAccountItemrange)
        {
            if (ModelState.IsValid)
            {
                var noteno = db.FinalAccountItemss.Where(ac => ac.FinalAccountItemsId == finalAccountItemrange.FinalAccountItemsId).FirstOrDefault();
                finalAccountItemrange.NoteNo = noteno.NoteNo;

                db.Entry(finalAccountItemrange).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("FinalAccount");
            }
            ViewBag.ClassificationId = new SelectList(db.FaClassificationCodes, "ClassificationId", "ClassificationCodeClassificationDesc", finalAccountItemrange.ClassificationId);
            ViewBag.FinalAccountItemsId = new SelectList(db.FinalAccountItemss, "FinalAccountItemsId", "NoteNoItemDescription", finalAccountItemrange.FinalAccountItemsId);
            return View(finalAccountItemrange);
        }

        // GET: IFMISTZ/FinalAccountItemranges/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FinalAccountItemrange finalAccountItemrange = db.FinalAccountItemranges.Find(id);
            if (finalAccountItemrange == null)
            {
                return HttpNotFound();
            }
            return View(finalAccountItemrange);
        }

        // POST: IFMISTZ/FinalAccountItemranges/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            FinalAccountItemrange finalAccountItemrange = db.FinalAccountItemranges.Find(id);
            db.FinalAccountItemranges.Remove(finalAccountItemrange);
            db.SaveChanges();
            return RedirectToAction("FinalAccount");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
