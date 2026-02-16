using PARAS.Api.Domain.Enums;

namespace PARAS.Api.DTOs;

public record ChangeLoanStatusRequest(
    LoanStatus ToStatus,
    string? admin,
    string? Comment
);

public record LoanStatusHistoryResponse(
    Guid Id,
    Guid LoanId,
    LoanStatus FromStatus,
    LoanStatus ToStatus,
    string? admin,
    string? Comment,
    DateTime ChangedAt
);