using Azure.Core;
using DSAR.Interfaces;
using DSAR.Models;
using DSAR.Repositories;
using DSAR.Repository;
using DSAR.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DSAR.Controllers
{
    [Authorize]
    public class RequestController : Controller
    {
        private readonly IRequestActionRepository _requestActionRepository;
        private readonly IRequestRepository _requestRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICaseStudyRepository _caseStudyRepository;
        private readonly UserManager<User> _userManager;
        private readonly iFormRepository _formRepo;
        private readonly IAppHistoryRepository _historyRepository;
        private readonly IApproveRepository _approveRepository;


        public RequestController(IRequestActionRepository requestActionRepository, IRequestRepository requestRepository, IUserRepository userRepository, ICaseStudyRepository caseStudyRepository, UserManager<User> userManager, iFormRepository formRepo, IAppHistoryRepository historyRepository, IApproveRepository approveRepository)
        {
            _requestActionRepository = requestActionRepository;
            _requestRepository = requestRepository;
            _userRepository = userRepository;
            _userManager = userManager;
            _caseStudyRepository = caseStudyRepository;
            _formRepo = formRepo;
            _historyRepository = historyRepository;
            _approveRepository = approveRepository;
        }

        public async Task<IActionResult> MyRequest()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            var requestAction = await _requestActionRepository.GetRequestsStillInProcessByUserId(userId); // Filtered list
            var viewModel = requestAction.Select(r => new RequestViewModel
            {
                RequestId = r.RequestId,
                FirstName = r.User?.FirstName,
                LastName = r.User?.LastName,
                StatusName = r.Status?.StatusName,
                DepartmentName = r.Department?.DepartmentName,
                RequestNumber = r.FormData.RequestNumber,
                LevelId = r.LevelId,
                ActionId = r.ActionId,

            }).ToList();

            return View(viewModel);
        }
        [Authorize]
        public IActionResult Request()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Request(RequestViewModel data)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            FormData request;
            bool emailResponse;
            const int initialStatusId = 1; // "new submission" status

            if (await _userManager.IsInRoleAsync(currentUser, "SectionManager"))
            {
                data.UserId = currentUser.Id;
                request = await _formRepo.HandleStep3Data(data, currentUser.UserId);
                _requestActionRepository.CreateSectionManager(data, currentUser, request);

                emailResponse = await _requestRepository.SendEmailAsync(data, currentUser);
                if (!emailResponse)
                {
                    // Handle false response  
                }
                ////////////////////history///////////////////////////////////////

                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    1,
                    "Request submitted"
                );

                ////////////////////history///////////////////////////////////////
                return RedirectToAction("Main", "Account");
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "DepartmentManager"))
            {
                data.UserId = currentUser.Id;
                request = await _formRepo.HandleStep3Data(data, currentUser.UserId);
                _requestActionRepository.CreateDepartmentManager(data, currentUser, request);

                emailResponse = await _requestRepository.SendEmailAsync(data, currentUser);
                if (!emailResponse)
                {
                    // Handle false response  
                }

                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    1,
                    "Request submitted"
                );
                return RedirectToAction("Main", "Account");
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "ITManager"))
            {
                data.UserId = currentUser.Id;
                request = await _formRepo.HandleStep3Data(data, currentUser.UserId);
                _requestActionRepository.CreateITManager(data, currentUser, request);

                emailResponse = await _requestRepository.SendEmailAsync(data, currentUser);
                if (!emailResponse)
                {
                    // Handle false response  
                }
                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    1,
                    "Request submitted"
                );
                return RedirectToAction("Main", "Account");
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "ApplicationManager"))
            {
                data.UserId = currentUser.Id;
                request = await _formRepo.HandleStep3Data(data, currentUser.UserId);
                _requestActionRepository.CreateITManager(data, currentUser, request);

                emailResponse = await _requestRepository.SendEmailAsync(data, currentUser);
                if (!emailResponse)
                {
                    // Handle false response  
                }
                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    1,
                    "Request submitted"
                );
                return RedirectToAction("Main", "Account");
            }
            data.UserId = currentUser.Id;
            request = await _formRepo.HandleStep3Data(data, currentUser.UserId);

            _requestActionRepository.Create(data, currentUser, request);

            emailResponse = await _requestRepository.SendEmailAsync(data, currentUser);
            if (!emailResponse)
            {
                // Handle false response  
            }
            ////////////////////history///////////////////////////////////////

            // "new submission" status
            await _historyRepository.CreateHistoryAsync(
                currentUser,
                request,
                initialStatusId,
                1,
                "Request submitted"
            );

            ////////////////////history///////////////////////////////////////
            return RedirectToAction("Main", "Account");
        }
        //New Form



        public async Task<IActionResult> OrderPage()
        {

            var currentUser = await _userManager.GetUserAsync(User);
            var userId = currentUser.Id;

            List<FormData> requests;


            if (await _userManager.IsInRoleAsync(currentUser, "DepartmentManager"))
            {
                requests = await _requestRepository.GetRequestsFromManagersInDepartmentAsync(userId, currentUser.DepartmentId ?? 0);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "SectionManager"))
            {
                requests = await _requestRepository.GetRequestsForManagerDepartmentAsync(userId, currentUser.SectionId ?? 0);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "ITManager"))
            {
                requests = await _requestRepository.GetRequestsForITManager(userId, currentUser.DepartmentId ?? 0);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "Analyzer"))
            {
                requests = await _requestRepository.GetRequestsForAnalyzer(userId, currentUser.DepartmentId ?? 0);
            }
            else if (await _userManager.IsInRoleAsync(currentUser, "ApplicationManager"))
            {
                requests = await _requestRepository.GetRequestsForApplicationManager(userId, currentUser.DepartmentId ?? 0);
            }
            else
            {
                return RedirectToAction("Account", "Main");
            }

            var viewModels = requests.Select(r => new RequestViewModel
            {
                RequestId = r.RequestId,
                LevelId = r.RequestActions.LevelId,
                FirstName = r.User.FirstName,
                LastName = r.User.LastName,
                DepartmentName = r.User.Department?.DepartmentName ?? "N/A",
                RequestNumber = r.RequestNumber,
                ActionId = r.RequestActions.ActionId,
            }).ToList();
            return View(viewModels);
        }

        public async Task<IActionResult> ViewSubmission(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var request = _requestRepository.GetById(id);
            if (request == null) return NotFound();

            var requestAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(request.RequestId);
            if (requestAction == null) return NotFound();
           

            await _requestActionRepository.ProtectViewPages(id, currentUser, request, requestAction);

            var viewModel = new RequestViewModel
            {
                RequestId = request.RequestId,
                Field1 = request.Field1,
                Field2 = request.Field2,
                Field3 = request.Field3,
                Depend = request.Depend,
                Field4 = request.Field4,
                Field5 = request.Field5,
                Field6 = request.Field6,
                Attachments = request.Attachments?.ToList()
            };

            return View(viewModel);
        }


        public async Task<IActionResult> ViewSubmission2(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var request = _requestRepository.GetById(id);
            if (request == null) return NotFound();

            var requestAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(request.RequestId);
            if (requestAction == null) return NotFound();

            int currentDepartmentId = currentUser.DepartmentId ?? 0;
            int requestDepartmentId = requestAction.DepartmentId;

            // Get roles
            bool isSectionManager = await _userManager.IsInRoleAsync(currentUser, "SectionManager");
            bool isDepartmentManager = await _userManager.IsInRoleAsync(currentUser, "DepartmentManager");
            bool isITManager = await _userManager.IsInRoleAsync(currentUser, "ITManager");
            bool isApplicationManager = await _userManager.IsInRoleAsync(currentUser, "ApplicationManager");
            bool isAnalyzer = await _userManager.IsInRoleAsync(currentUser, "Analyzer");
            bool isUser = await _userManager.IsInRoleAsync(currentUser, "User");

            var form = await _formRepo.GetById(id);
            if (form == null) return NotFound();

            // ✅ Allow user to view only their own request
            if (isUser && form.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // ✅ Check department match for all managers/analyzers
            if ((isSectionManager || isDepartmentManager || isITManager || isAnalyzer) &&
                requestDepartmentId != currentDepartmentId)
            {
                return Forbid();
            }

            var histories = await _historyRepository.GetHistoriesByRequestIdAsync(request.RequestId);
            var lastHistory = histories.OrderByDescending(h => h.CreationDate).FirstOrDefault();
            bool isLastActor = lastHistory != null && lastHistory.UserId == currentUser.Id;

            // ... your role checks ...

            if (isSectionManager && requestAction.LevelId != 1 && !isLastActor)
                return Forbid();

            if (isDepartmentManager && requestAction.LevelId != 2 && !isLastActor)
                return Forbid();

            if (isITManager && !(requestAction.LevelId == 3 || requestAction.LevelId == 7) && !isLastActor)
                return Forbid();

            if (isApplicationManager && !(requestAction.LevelId == 4 || requestAction.LevelId == 6) && !isLastActor)
                return Forbid();

            if (isAnalyzer && requestAction.LevelId != 5 && !isLastActor)
                return Forbid();

            // Get the last history entry for this request

            // ✅ Final fallback check: allow if user is in any allowed role OR is the last actor
            if (!isUser && !isSectionManager && !isDepartmentManager && !isITManager && !isApplicationManager && !isAnalyzer && !isLastActor)
            {
                return Forbid();
            }


            var viewModel = new RequestViewModel
            {
                RequestId = form.RequestId,
                Name = form.Name,
                Email = form.Email,
                RepeatLimit = form.RepeatLimit,
                Fees = form.Fees,
                Cities = form.Cities,
                TargetAudience = form.TargetAudience,
                DepName = form.DepName,
                ExpectedOutput1 = form.ExpectedOutput1,
                ExpectedOutput2 = form.ExpectedOutput2,
                ApprovedTemplate = form.ApprovedTemplate,
                DetailedInfo = form.DetailedInfo,
                RequiredConditions = form.RequiredConditions,
                Attachments = form.Attachments?.ToList() // ✅ Add this line


            };
            return viewModel == null ? NotFound() : View(viewModel);
        }
        public async Task<IActionResult> ViewSubmission3(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var request = _requestRepository.GetById(id);
            if (request == null) return NotFound();

            var requestAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(request.RequestId);
            if (requestAction == null) return NotFound();

         
            int currentDepartmentId = currentUser.DepartmentId ?? 0;
            int requestDepartmentId = requestAction.DepartmentId;

            // Get roles
            bool isSectionManager = await _userManager.IsInRoleAsync(currentUser, "SectionManager");
            bool isDepartmentManager = await _userManager.IsInRoleAsync(currentUser, "DepartmentManager");
            bool isITManager = await _userManager.IsInRoleAsync(currentUser, "ITManager");
            bool isApplicationManager = await _userManager.IsInRoleAsync(currentUser, "ApplicationManager");
            bool isAnalyzer = await _userManager.IsInRoleAsync(currentUser, "Analyzer");
            bool isUser = await _userManager.IsInRoleAsync(currentUser, "User");

            var form = await _formRepo.GetById(id);
            if (form == null) return NotFound();

            // ✅ Allow user to view only their own request
            if (isUser && form.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // ✅ Check department match for all managers/analyzers
            if ((isSectionManager || isDepartmentManager || isITManager || isAnalyzer) &&
                requestDepartmentId != currentDepartmentId)
            {
                return Forbid();
            }

            var histories = await _historyRepository.GetHistoriesByRequestIdAsync(request.RequestId);
            var lastHistory = histories.OrderByDescending(h => h.CreationDate).FirstOrDefault();
            bool isLastActor = lastHistory != null && lastHistory.UserId == currentUser.Id;

            // ... your role checks ...

            if (isSectionManager && requestAction.LevelId != 1 && !isLastActor)
                return Forbid();

            if (isDepartmentManager && requestAction.LevelId != 2 && !isLastActor)
                return Forbid();

            if (isITManager && !(requestAction.LevelId == 3 || requestAction.LevelId == 7) && !isLastActor)
                return Forbid();

            if (isApplicationManager && !(requestAction.LevelId == 4 || requestAction.LevelId == 6) && !isLastActor)
                return Forbid();

            if (isAnalyzer && requestAction.LevelId != 5 && !isLastActor)
                return Forbid();

            // Get the last history entry for this request

            // ✅ Final fallback check: allow if user is in any allowed role OR is the last actor
            if (!isUser && !isSectionManager && !isDepartmentManager && !isITManager && !isApplicationManager && !isAnalyzer && !isLastActor)
            {
                return Forbid();
            }


            var viewModel = new RequestViewModel
            {
                RequestId = form.RequestId,
                Workflow = form.Workflow,
                UploadsRequired = form.UploadsRequired,
                Documents = form.Documents,
                Timeline = form.Timeline,
                SystemNeeded = form.SystemNeeded,
                Cities2 = form.Cities2,
                DepartmentHeadName = form.DepartmentHeadName,
                AdditionalNotes = form.AdditionalNotes,
                Attachments = form.Attachments?.ToList() // ✅ Add this line

            };
            return viewModel == null ? NotFound() : View(viewModel);
        }
        public async Task<IActionResult> StepDescriptionsView(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var request = _requestRepository.GetById(id);
            if (request == null) return NotFound();

            var requestAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(request.RequestId);
            if (requestAction == null) return NotFound();

            int currentDepartmentId = currentUser.DepartmentId ?? 0;
            int requestDepartmentId = requestAction.DepartmentId;

            // Get roles
            bool isSectionManager = await _userManager.IsInRoleAsync(currentUser, "SectionManager");
            bool isDepartmentManager = await _userManager.IsInRoleAsync(currentUser, "DepartmentManager");
            bool isITManager = await _userManager.IsInRoleAsync(currentUser, "ITManager");
            bool isApplicationManager = await _userManager.IsInRoleAsync(currentUser, "ApplicationManager");
            bool isAnalyzer = await _userManager.IsInRoleAsync(currentUser, "Analyzer");
            bool isUser = await _userManager.IsInRoleAsync(currentUser, "User");

            var form = await _formRepo.GetById(id);
            if (form == null) return NotFound();

            // ✅ Allow user to view only their own request
            if (isUser && form.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // ✅ Check department match for all managers/analyzers
            if ((isSectionManager || isDepartmentManager || isITManager || isAnalyzer) &&
                requestDepartmentId != currentDepartmentId)
            {
                return Forbid();
            }

            var histories = await _historyRepository.GetHistoriesByRequestIdAsync(request.RequestId);
            var lastHistory = histories.OrderByDescending(h => h.CreationDate).FirstOrDefault();
            bool isLastActor = lastHistory != null && lastHistory.UserId == currentUser.Id;

            // ... your role checks ...

            if (isSectionManager && requestAction.LevelId != 1 && !isLastActor)
                return Forbid();

            if (isDepartmentManager && requestAction.LevelId != 2 && !isLastActor)
                return Forbid();

            if (isITManager && !(requestAction.LevelId == 3 || requestAction.LevelId == 7) && !isLastActor)
                return Forbid();

            if (isApplicationManager && !(requestAction.LevelId == 4 || requestAction.LevelId == 6) && !isLastActor)
                return Forbid();

            if (isAnalyzer && requestAction.LevelId != 5 && !isLastActor)
                return Forbid();

            // Get the last history entry for this request

            // ✅ Final fallback check: allow if user is in any allowed role OR is the last actor
            if (!isUser && !isSectionManager && !isDepartmentManager && !isITManager && !isApplicationManager && !isAnalyzer && !isLastActor)
            {
                return Forbid();
            }

            var formData = await _formRepo.GetDescriptionsByRequestId(id);
            if (formData == null) return NotFound();

            // Create the proper view model  
            var viewModel = new RequestViewModel
            {
                FormId = id, // Pass the form ID for navigation  
                Descriptions = formData?.ToList() ?? new List<DescriptionEntry>(),
                ActionId = requestAction?.ActionId ?? 0, // Fix: Use null-coalescing operator to handle null reference  
                LevelId = requestAction?.LevelId ?? 0,
            };

            return View(viewModel);
        }

        // STEP 1 - GET
        public async Task<IActionResult> Step1()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var requests = await _requestActionRepository.GetRequestsStillInProcessByUserId(currentUser.Id);
            if (requests.Count! > 0)
            {
                TempData["Error"] = "You already have a pending request. Please wait until it is completed before submitting a new one.";
                return RedirectToAction("Main", "Account");
            }
            return View(await _formRepo.GetCurrentFormData());
        }

        // STEP 1 - POST
        [HttpPost]
        public async Task<IActionResult> Step1(RequestViewModel data, IFormFile Attachment)
        {
            await _formRepo.HandleStep1Data(data, Attachment);
            return RedirectToAction("Step2");
        }

        // STEP 2 - GET
        public async Task<IActionResult> Step2()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var requests = await _requestActionRepository.GetRequestsStillInProcessByUserId(currentUser.Id);
            if (requests.Count! > 0)
            {
                TempData["Error"] = "You already have a pending request. Please wait until it is completed before submitting a new one.";
                return RedirectToAction("Main", "Account");
            }
            return View(await _formRepo.GetCurrentFormData());
        }

        // STEP 2 - POST
        [HttpPost]
        public async Task<IActionResult> Step2(RequestViewModel data, IFormFile Attachment1, IFormFile Attachment2)
        {
            await _formRepo.HandleStep2Data(data, Attachment1, Attachment2);
            return RedirectToAction("Step4");
        }

        // STEP 3 - GET
        public async Task<IActionResult> Step3()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var requests = await _requestActionRepository.GetRequestsStillInProcessByUserId(currentUser.Id);
            if (requests.Count! > 0)
            {
                TempData["Error"] = "You already have a pending request. Please wait until it is completed before submitting a new one.";
                return RedirectToAction("Main", "Account");
            }
            return View(await _formRepo.GetCurrentFormData());
        }

        // STEP 3 - POST

        //public async Task<IActionResult> Step3(RequestViewModel data)
        //{
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    data.Id = currentUser.Id;

        //    await _formRepo.HandleStep3Data(data, currentUser.UserId);
        //    return RedirectToAction("OrderPage");

        //}

        // STEP 4 - GET
        [HttpGet]
        public async Task<IActionResult> Step4()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var requests = await _requestActionRepository.GetRequestsStillInProcessByUserId(currentUser.Id);
            if (requests.Count! > 0)
            {
                TempData["Error"] = "You already have a pending request. Please wait until it is completed before submitting a new one.";
                return RedirectToAction("Main", "Account");
            }
            return View(await _formRepo.GetCurrentFormData());
        }

        // STEP 4 - POST
        [HttpPost]
        public async Task<IActionResult> Step4(
            RequestViewModel data,
            IFormFile WorkflowFile,
            IFormFile UploadsRequiredFile,
            IFormFile DocumentsFile)
        {
            await _formRepo.HandleStep4Data(
                data,
                WorkflowFile,
                UploadsRequiredFile,
                DocumentsFile
            );
            return RedirectToAction("StepDescriptions");
        }

        // STEP DESCRIPTIONS - GET
        [HttpGet]
        public async Task<IActionResult> StepDescriptions()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();
            var requests = await _requestActionRepository.GetRequestsStillInProcessByUserId(currentUser.Id);
            if (requests.Count! > 0)
            {
                TempData["Error"] = "You already have a pending request. Please wait until it is completed before submitting a new one.";
                return RedirectToAction("Main", "Account");
            }
            var descriptions = await _formRepo.GetDescriptions();
            if (descriptions.Count == 0)
            {
                descriptions.Add(new DescriptionEntry());
            }
            // Example mapping: create new DescriptionEntry objects (or a custom view model if you have one)
            var mappedDescriptions = descriptions.Select(d => new DescriptionEntry
            {

                Description1 = d.Description1,
                Description2 = d.Description2,

            }).ToList();
            return View(new RequestViewModel
            {
                Descriptions = descriptions
            });
        }

        // STEP DESCRIPTIONS - POST
        [HttpPost]
        public async Task<IActionResult> StepDescriptions(RequestViewModel model)
        {
            await _formRepo.HandleDescriptions(model.Descriptions);
            return RedirectToAction("Step3");
        }

        public IActionResult Complete()
        {
            return View();
        }
        public async Task<IActionResult> DownloadAttachment(int attachmentId)
        {
            var attachment = await _formRepo.GetAttachmentById(attachmentId);
            if (attachment == null) return NotFound();

            return File(attachment.Data, "application/octet-stream",
                       $"{attachment.AttachmentMetadata.FileName}{attachment.AttachmentMetadata.FileExtension}");
        }
        public async Task<IActionResult> Submissions()
        {
            return View(await _formRepo.GetAll());
        }

        // Clear snapshot (for testing/debugging)
        public async Task<IActionResult> ClearSnapshot()
        {
            await _formRepo.ClearCurrentSnapshot();
            return RedirectToAction("Step1");
        }

       
        
        [HttpGet]
        public async Task<IActionResult> ApprovePageAsync(int requestId, int actionId)
        {
            var requestAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(requestId);
            var currentUser = await _userManager.GetUserAsync(User);
            int currentDepartmentId = currentUser.DepartmentId ?? 0;
            int requestDepartmentId = requestAction.DepartmentId;

            bool isSectionManager = await _userManager.IsInRoleAsync(currentUser, "SectionManager");
            bool isDepartmentManager = await _userManager.IsInRoleAsync(currentUser, "DepartmentManager");
            bool isITManager = await _userManager.IsInRoleAsync(currentUser, "ITManager");
            bool isApplicationManager = await _userManager.IsInRoleAsync(currentUser, "ApplicationManager");
            bool isAnalyzer = await _userManager.IsInRoleAsync(currentUser, "Analyzer");
            bool isUser = await _userManager.IsInRoleAsync(currentUser, "User");

            var form = await _formRepo.GetById(requestId);
            if (form == null) return NotFound();

            // ✅ Allow user to view only their own request
            if (isUser && form.UserId != currentUser.Id)
            {
                return Forbid();
            }

            // ✅ Check department match for all managers/analyzers
            if ((isSectionManager || isDepartmentManager || isITManager || isAnalyzer) &&
                requestDepartmentId != currentDepartmentId)
            {
                return Forbid();
            }

            // ✅ Check LevelId permissions
            if (isSectionManager && requestAction.LevelId != 1)
                return Forbid();

            if (isDepartmentManager && requestAction.LevelId != 2)
                return Forbid();

            if (isITManager && !(requestAction.LevelId == 3 || requestAction.LevelId == 7))
                return Forbid();

            if (isApplicationManager && !(requestAction.LevelId == 4 || requestAction.LevelId == 6))
                return Forbid();

            if (isAnalyzer && requestAction.LevelId != 5)
                return Forbid();

            // ✅ Final fallback check
            if (!isUser && !isSectionManager && !isDepartmentManager && !isITManager && !isApplicationManager && !isAnalyzer)
            {
                return Forbid();
            }
            var request = _requestRepository.GetById(requestId);
            if (request == null)
                return NotFound();

            var caseStudy = await _caseStudyRepository.GetByRequestIdAsync(requestId);

            List<CaseStudyAttachmentMetadata> attachments = null;
            if (caseStudy != null)
            {
                attachments = await _caseStudyRepository.GetAttachmentsByCaseStudyIdAsync(caseStudy.CaseId);
            }
            ViewBag.Attachments = attachments;

            var action = await _requestActionRepository.GetByIdAsync(actionId);
            if (action == null)
                return NotFound();

            var viewModel = new RequestViewModel
            {
                RequestId = request.RequestId,
                ActionId = actionId,
                // ServiceName = request.ServiceName,
                SectionNotes = request.SectionNotes,
                DepartmentNotes = request.DepartmentNotes,

                WorkTeam = caseStudy?.WorkTeam,
                Notes = caseStudy?.Notes,
                restriction = caseStudy?.restriction,
                LevelId = action.LevelId
            };

            return View(viewModel);
        }




        [HttpPost]
        public async Task<IActionResult> Approve(RequestViewModel model, int actionId, int requestId, string decision)
        {

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Account", "Main");

            var request = _requestRepository.GetById(requestId);
            if (request == null)
                return RedirectToAction("Account", "Main");

            if (await _userManager.IsInRoleAsync(currentUser, "DepartmentManager"))
            {
                await _approveRepository.ApproveRequestByDepartmentManager(model, actionId, requestId, decision, currentUser, request);

            }
            else if (await _userManager.IsInRoleAsync(currentUser, "SectionManager"))
            {
                await _approveRepository.ApproveRequestBySectionManager(model, actionId, requestId, decision, currentUser, request);

            }

            else if (await _userManager.IsInRoleAsync(currentUser, "ITManager"))
            {
                await _approveRepository.ApproveRequestByITManager(model, actionId, requestId, decision, currentUser, request);

            }
            else if (await _userManager.IsInRoleAsync(currentUser, "ApplicationManager"))
            {
                await _approveRepository.ApproveRequestByApplicationManager(model, actionId, requestId, decision, currentUser, request);

            }
           
            return RedirectToAction("Main", "Account");

        }



        public async Task<IActionResult> AnalyzerUsers(int requestId)
        {
            var analyzers = await _userManager.GetUsersInRoleAsync("Analyzer");

            var viewModels = analyzers.Select(user => new CaseStudyViewModel
            {
                Id = user.Id,
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RequestId = requestId // Use the parameter here
            }).ToList();

            return View(viewModels);
        }

        public async Task<IActionResult> ConfirmAnalyzer(int requestId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var request = _requestRepository.GetById(requestId);
            if (request == null)
                return NotFound();
            _caseStudyRepository.CreateCaseStudy(userId, requestId);

            var existingAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(requestId);

            if (existingAction != null)
            {

                existingAction.StatusId = 5;
                existingAction.LevelId = 5;
                _requestActionRepository.Update(existingAction);
                //history
                const int initialStatusId = 2; // "in progress" status
                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    5,
                    "Request approved initially by ITManager"
                );
                //history
            }

            return RedirectToAction("Main", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> CaseStudy(int requestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var caseStudy = await _caseStudyRepository.GetCaseStudyByUserIdAsync(currentUser.Id, requestId);
            if (caseStudy == null) return NotFound();

            var request = await _requestRepository.GetByIdAsync(requestId);

            var attachments = await _caseStudyRepository.GetAttachmentsByCaseStudyIdAsync(caseStudy.CaseId);

            var viewModel = new CaseStudyViewModel
            {
                CaseId = caseStudy.CaseId,
                RequestId = caseStudy.RequestId,
                UserId = caseStudy.UserId,
                WorkTeam = caseStudy.WorkTeam,
                restriction = caseStudy.restriction,
                Notes = caseStudy.Notes,
                CreatedAt = caseStudy.CreatedAt,
                SectionNotes = request?.SectionNotes,
                DepartmentNotes = request?.DepartmentNotes,

                Attachments = attachments.Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileExtension = a.FileExtension
                }).ToList()
            };

            return View(viewModel);
        }


        [HttpPost]
        [Authorize(Roles = "Analyzer")]
        public async Task<IActionResult> CaseStudy(CaseStudyViewModel model, IFormFile CaseStudyAttachment)
        {
            var caseStudy = await _caseStudyRepository.GetByIdAsync(model.CaseId);
            if (caseStudy == null) RedirectToAction("Account", "Main");


            caseStudy.WorkTeam = model.WorkTeam;
            caseStudy.restriction = model.restriction;
            caseStudy.Notes = model.Notes;
            caseStudy.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


            if (CaseStudyAttachment != null && CaseStudyAttachment.Length > 0)
            {
                await _caseStudyRepository.AddAttachmentAsync(caseStudy.CaseId, CaseStudyAttachment);
            }

            _caseStudyRepository.Update(caseStudy);

            var requestId = caseStudy.RequestId;
            var request = _requestRepository.GetById(requestId);
            if (request == null) RedirectToAction("Account", "Main");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) RedirectToAction("Account", "Main");

            var existingAction = await _requestActionRepository.GetRequestActionByRequestIdAsync(requestId);

            if (existingAction != null)
            {
                existingAction.LevelId = 6;
                existingAction.StatusId = 2;
                _requestActionRepository.Update(existingAction);
                // history

                const int initialStatusId = 2; // "in progress" status
                await _historyRepository.CreateHistoryAsync(
                    currentUser,
                    request,
                    initialStatusId,
                    6,
                    "Request studied by Analyzer"
                );
                // history
            }

            return RedirectToAction("Main", "Account");
        }
        public async Task<IActionResult> CancelRequest(int actionId)
        {

            var action = await _requestActionRepository.GetByIdAsync(actionId);
            if (action == null) return RedirectToAction("Main", "Account");

            if (action.LevelId == 1)
            {
                action.StatusId = 3; // Approved
                action.LevelId = 9;
                _requestActionRepository.Update(action);
            }

            return RedirectToAction("Main", "Account");
        }

        public async Task<IActionResult> DownloadCaseStudyAttachment(int attachmentId)
        {
            var attachment = await _caseStudyRepository.GetAttachmentDataByIdAsync(attachmentId);
            if (attachment == null) return NotFound();

            var meta = attachment.CaseStudyAttachmentMetadata;

            return File(attachment.Data, "application/octet-stream", $"{meta.FileName}{meta.FileExtension}");
        }

        public async Task<IActionResult> CompeleteRequest()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            var requestAction = await _requestActionRepository.GetCompeleteRequestsByUserId(userId); // Filtered list
            var viewModel = requestAction.Select(r => new RequestViewModel
            {
                RequestId = r.RequestId,
                StatusName = r.Status?.StatusName,
                DepartmentName = r.Department?.DepartmentName,
                RequestNumber = r.FormData.RequestNumber,
                LevelId = r.LevelId,
                ActionId = r.ActionId

            }).ToList();

            return View(viewModel);
        }

        public IActionResult GetAllRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            var requestAction = _requestActionRepository.GetAllByUserId(userId); // Filtered list
            var viewModel = requestAction.Select(r => new RequestViewModel
            {
                RequestId = r.RequestId,
                StatusName = r.Status?.StatusName,
                DepartmentName = r.Department?.DepartmentName,
                RequestNumber = r.FormData.RequestNumber,
                LevelId = r.LevelId,
                ActionId = r.ActionId

            }).ToList();

            return View(viewModel);
        }
        public async Task<IActionResult> CompeleteRequestManager()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            var requestAction = await _historyRepository.GetHistroyRequestsByUserId(userId); // Filtered list
            var viewModel = requestAction.Select(r => new HistoryViewModel
            {
                RequestId = r.RequestId,
                StatusName = r.Status?.StatusName,
                // DepartmentName = r.Department?.DepartmentName,
                RequestNumber = r.FormData.RequestNumber,
                LevelId = r.LevelId,
                // ActionId = r.ActionId

            }).ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> GetAllRequestsManager()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user's ID

            var requestAction = await _historyRepository.GetHistroyRequestsByUserId(userId); // Filtered list
            var viewModel = requestAction.Select(r => new HistoryViewModel
            {
                RequestId = r.RequestId,
                StatusName = r.Status?.StatusName,
                // DepartmentName = r.Department?.DepartmentName,
                RequestNumber = r.FormData.RequestNumber,
                LevelId = r.LevelId,
                // ActionId = r.ActionId

            }).ToList();

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> History(int requestId)
        {

            var histories = await _historyRepository.GetHistoriesByRequestIdAsync(requestId);


            var historiesVm = histories.Select(h => new HistoryViewModel
            {
                CreationDate = h.CreationDate,
                LevelName = h.Levels.LevelName,
                StatusName = h.Status.StatusName,
                RoleName = h.Role.Name,
                Information = h.Information
            })
            .ToList();


            ViewBag.RequestId = requestId;


            return View(historiesVm);
        }

    }


}
