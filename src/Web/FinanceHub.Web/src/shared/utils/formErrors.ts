import { ApiError } from '../types/api.types';
import type { UseFormSetError, FieldValues, Path } from 'react-hook-form';

/**
 * Mapeia erros campo a campo do FluentValidation (.NET 10) para o React Hook Form,
 * normalizando de PascalCase para camelCase.
 */
export function mapProblemDetailsToFormErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>
): boolean {
  if (error instanceof ApiError && error.problemDetails.errors) {
    const { errors } = error.problemDetails;
    let hasFieldErrors = false;

    Object.entries(errors).forEach(([field, messages]) => {
      if (messages && messages.length > 0) {
        const normalizedFieldName = (field.charAt(0).toLowerCase() + field.slice(1)) as Path<T>;
        setError(normalizedFieldName, {
          type: 'server',
          message: messages[0],
        });
        hasFieldErrors = true;
      }
    });

    return hasFieldErrors;
  }

  return false;
}
