using Microsoft.AspNetCore.Mvc;
using Pay1193.Services;
using Pay1193.Entity;
using Pay1193.Models;
using Pay1193.Services.Implement;

namespace Pay1193.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPayService _paymentService;
        private readonly INationalInsuranceService _insuranceService;
        private readonly ITaxService _taxService;
        private readonly IEmployee _employeeService;

        private decimal overtimeHrs;
        private decimal contractualEarnings;
        private decimal overtimeEarnings;
        private decimal totalEarnings;
        private decimal tax;
        private decimal unionFee;
        private decimal studentLoan;
        private decimal nationalInsurance;
        private decimal totalDeduction;

        private readonly IWebHostEnvironment _webHostEnvironment;
        public PaymentController(IPayService paymentService,
                                 INationalInsuranceService insuranceService,
                                 ITaxService taxService,
                                 IEmployee employeeService)
        {
            _paymentService = paymentService;
            _insuranceService = insuranceService;
            _taxService = taxService;
            _employeeService = employeeService;
        }

        public IActionResult Index()
        {
            var payRecords = _paymentService.GetAll();
            var payRecordsToView = payRecords.Select(payment => new PaymentIndexViewModel
            {
                Id = payment.Id,
                EmployeeId = payment.EmployeeId,
                FullName = payment.FullName,
                PayDate = payment.DatePay,
                PayMonth = payment.MonthPay,
                TaxYearId = payment.TaxYearId,
                Year = _paymentService.GetTaxYearById(payment.TaxYearId).YearOfTax,
                TotalEarnings = payment.TotalEarnings,
                TotalDeduction = payment.EarningDeduction,
                NetPayment = payment.NetPayment,
                Employee = payment.Employee
            });
            

            return View(payRecordsToView);
        }

        

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.taxYears = _paymentService.GetAllTaxYear();
            var model = new PaymentCreateViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PaymentCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var payment = new PaymentRecord
                {
                    Id = model.Id,
                    EmployeeId = model.EmployeeId,
                    FullName = _employeeService.GetById(model.EmployeeId).FullName,
                    NiNo = _employeeService.GetById(model.EmployeeId).NationalInsuranceNo,
                    DatePay = model.PayDate,
                    MonthPay = model.PayMonth,
                    TaxYearId = model.TaxYearId,
                    TaxCode = model.TaxCode,
                    HourlyRate = model.HourlyRate,
                    HourWorked = model.HoursWorked,
                    ContractualHours = model.ContractualHours,
                    OvertimeHours = overtimeHrs = _paymentService.OverTimeHours(model.HoursWorked, model.ContractualHours),
                    ContractualEarnings = contractualEarnings = _paymentService.ContractualEarning(model.ContractualHours, model.HoursWorked, model.HourlyRate),
                    OvertimeEarnings = overtimeEarnings = _paymentService.OvertimeEarnings(_paymentService.OvertimeRate(model.HourlyRate), overtimeHrs),
                    TotalEarnings = totalEarnings = _paymentService.TotalEarnings(overtimeEarnings, contractualEarnings),
                    Tax = tax = _taxService.TaxAmount(totalEarnings),
                    UnionFee = unionFee = _employeeService.UnionFee(model.EmployeeId),
                    SLC = studentLoan = _employeeService.StudentLoanRepaymentAmount(model.EmployeeId, totalEarnings),
                    NiC = nationalInsurance = _insuranceService.NIContribution(totalEarnings),
                    EarningDeduction = totalDeduction = _paymentService.TotalDeduction(tax, nationalInsurance, studentLoan, unionFee),
                    NetPayment = _paymentService.NetPay(totalEarnings, totalDeduction)
                };
                
                await _paymentService.CreateAsync(payment);
                return RedirectToAction("Index");
            }
            ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.taxYears = _paymentService.GetAllTaxYear();

            return View(); 
        }

        public IActionResult Detail(int id)
        {
            var payment = _paymentService.GetById(id);
            if (payment == null)
            {
                return NotFound();
            }
            PaymentDetailsViewModel model = new PaymentDetailsViewModel()
            {
                Id = payment.Id,
                EmployeeId = payment.EmployeeId,
                FullName = payment.FullName,
                NiNo = payment.NiNo,
                PayDate = payment.DatePay,
                PayMonth = payment.MonthPay,
                TaxYearId = payment.TaxYearId,
                Year = _paymentService.GetTaxYearById(payment.TaxYearId).YearOfTax,
                TaxCode = payment.TaxCode,
                HourlyRate = payment.HourlyRate,
                HoursWorked = payment.HourWorked,
                ContractualHours = payment.ContractualHours,
                OvertimeHours = payment.OvertimeHours,
                OvertimeRate = _paymentService.OvertimeRate(payment.HourlyRate),
                ContractualEarnings = payment.ContractualEarnings,
                OvertimeEarnings = payment.OvertimeEarnings,
                Tax = payment.Tax,
                NIC = payment.NiC,
                UnionFee = payment.UnionFee,
                SLC = payment.SLC,
                TotalEarnings = payment.TotalEarnings,
                TotalDeduction = payment.EarningDeduction,
                Employee = payment.Employee,
                TaxYear = payment.TaxYear,
                NetPayment = payment.NetPayment
            };
            ViewBag.employees = _employeeService.GetAllEmployeesForPayroll();
            ViewBag.taxYears = _paymentService.GetAllTaxYear();
            return View(model);
        }

        public IActionResult Payslip(int id)
        {
            var paymentRecord = _paymentService.GetById(id);
            if (paymentRecord == null)
            {
                return NotFound();
            }

            var model = new PaymentDetailsViewModel()
            {
                Id = paymentRecord.Id,
                EmployeeId = paymentRecord.EmployeeId,
                FullName = paymentRecord.FullName,
                NiNo = paymentRecord.NiNo,
                PayDate = paymentRecord.DatePay,
                PayMonth = paymentRecord.MonthPay,
                TaxYearId = paymentRecord.TaxYearId,
                Year = _paymentService.GetTaxYearById(paymentRecord.TaxYearId).YearOfTax,
                TaxCode = paymentRecord.TaxCode,
                HourlyRate = paymentRecord.HourlyRate,
                HoursWorked = paymentRecord.HourWorked,
                ContractualHours = paymentRecord.ContractualHours,
                OvertimeHours = paymentRecord.OvertimeHours,
                OvertimeRate = _paymentService.OvertimeRate(paymentRecord.HourlyRate),
                ContractualEarnings = paymentRecord.ContractualEarnings,
                OvertimeEarnings = paymentRecord.OvertimeEarnings,
                Tax = paymentRecord.Tax,
                NIC = paymentRecord.NiC,
                UnionFee = paymentRecord.UnionFee,
                SLC = paymentRecord.SLC,
                TotalEarnings = paymentRecord.TotalEarnings,
                TotalDeduction = paymentRecord.EarningDeduction,
                Employee = paymentRecord.Employee,
                TaxYear = paymentRecord.TaxYear,
                NetPayment = paymentRecord.NetPayment
            };

            return View(model);
        }


    }
}
