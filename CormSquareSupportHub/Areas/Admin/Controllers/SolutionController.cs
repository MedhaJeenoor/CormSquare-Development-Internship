using Microsoft.AspNetCore.Mvc;
using SupportHub.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SupportHub.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc.Rendering;
using SupportHub.Models.ViewModels;

namespace CormSquareSupportHub.Controllers
{
    public class SolutionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public SolutionController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            List<Solution> solutions = (await _unitOfWork.Solution
                .GetAllAsync(filter: c => !c.IsDeleted))
                .OrderBy(c => c.DocumentId)
                .ToList();

            return View(solutions);
        }



        public async Task<IActionResult> Create()
        {
            var categories = await _unitOfWork.Category.GetAllAsync();

            var viewModel = new SolutionVM
            {
                Solution = new Solution(),
                Categories = categories.ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolutionVM viewModel)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Solution.Add(viewModel.Solution);
                await _unitOfWork.SaveAsync();
                return RedirectToAction(nameof(Index));
            }

            // Reload categories if the model is invalid
            viewModel.Categories = (await _unitOfWork.Category.GetAllAsync()).ToList();
            return View(viewModel);
        }

    }
}
