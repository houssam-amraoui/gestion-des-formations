using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Application.Categories;
using TrainingManagement.Domain.Constants;
using TrainingManagement.Web.ViewModels.Categories;

namespace TrainingManagement.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = AppRoles.Admin)]
public sealed class CategoriesController(ICategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var results = await categoryService.GetPagedAsync(new(search, page));
        return View(new CategoryListViewModel { Search = search, Results = results });
    }

    public async Task<IActionResult> Details(int id)
    {
        var category = await categoryService.GetByIdAsync(id);
        return category is null ? NotFound() : View(new CategoryDetailsViewModel { Category = category });
    }

    [HttpGet]
    public IActionResult Create() => View(new CategoryCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await categoryService.CreateAsync(ToInput(model));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["SuccessMessage"] = "La catégorie a été créée.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await categoryService.GetByIdAsync(id);
        if (category is null) return NotFound();
        return View(new CategoryEditViewModel
        {
            Id = category.Id, Name = category.Name, Slug = category.Slug,
            Description = category.Description, ImageUrl = category.ImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        var result = await categoryService.UpdateAsync(id, ToInput(model));
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }
        TempData["SuccessMessage"] = "La catégorie a été mise à jour.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        SetMessage(await categoryService.ToggleStatusAsync(id), "Le statut de la catégorie a été modifié.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        SetMessage(await categoryService.DeleteAsync(id), "La catégorie a été supprimée.");
        return RedirectToAction(nameof(Index));
    }

    private void SetMessage(TrainingManagement.Application.Common.ServiceResult result, string success)
    {
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Succeeded ? success : result.Error;
    }

    private static CategoryInput ToInput(CategoryCreateViewModel model) =>
        new(model.Name, model.Slug, model.Description, model.ImageUrl);
}
