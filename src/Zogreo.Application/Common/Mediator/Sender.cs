using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Zogreo.Application.Common.Mediator;

public class Sender(IServiceProvider sp) : ISender
{
    public async Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        // Run validation behavior if a validator is registered
        var validatorType = typeof(IValidator<>).MakeGenericType(command.GetType());
        var validator = sp.GetService(validatorType) as IValidator;
        if (validator != null)
        {
            var ctx = new ValidationContext<object>(command);
            var result = await validator.ValidateAsync(ctx, ct);
            if (!result.IsValid)
                throw new Exceptions.ValidationException(result.Errors);
        }

        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = sp.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)command, ct);
    }

    public async Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        dynamic handler = sp.GetRequiredService(handlerType);
        return await handler.Handle((dynamic)query, ct);
    }
}
