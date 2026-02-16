using PARAS.Api.Domain.Entities;
using PARAS.Api.Domain.Enums;

namespace PARAS.Api.Services;

public static class LoanStatusRules
{
    public static bool IsValidTransition(LoanStatus from, LoanStatus to) =>
        from switch
        {
            LoanStatus.pending => to is LoanStatus.approved or LoanStatus.rejected or LoanStatus.cancelled,
            LoanStatus.approved => to is LoanStatus.cancelled,
            LoanStatus.rejected => false,
            LoanStatus.cancelled => false,
            _ => false
        };
}