using Microsoft.EntityFrameworkCore;
using Zogreo.Application.Common.Exceptions;
using Zogreo.Application.Common.Interfaces;
using Zogreo.Application.Common.Mediator;
using Zogreo.Application.Features.Admin.DTOs;
using Zogreo.Domain.Enums;

namespace Zogreo.Application.Features.Admin.Commands;

public record UpdateStudentStatusCommand(Guid Id, StudentStatus Status) : ICommand<AdminStudentItem>;

public class UpdateStudentStatusCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateStudentStatusCommand, AdminStudentItem>
{
    public async Task<AdminStudentItem> Handle(UpdateStudentStatusCommand cmd, CancellationToken ct)
    {
        var student = await db.Students
            .Include(s => s.User).Include(s => s.Application).ThenInclude(a => a.Program)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw AppException.NotFound("Student not found.");

        student.Status = cmd.Status;
        await db.SaveChangesAsync(ct);

        return new AdminStudentItem(student.Id, student.AdmissionNumber, student.User.FullName, student.User.Email, student.Application.Program.Name, student.Status.ToString(), student.EnrolledAt);
    }
}
