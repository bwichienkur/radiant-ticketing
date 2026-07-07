import { Children, cloneElement, isValidElement, type ReactElement, type ReactNode } from 'react';

interface FormFieldProps {
  id: string;
  label: string;
  hint?: string;
  error?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
}

function enhanceControl(
  child: ReactNode,
  error: string | undefined,
  describedBy: string | undefined,
): ReactNode {
  if (!isValidElement(child)) {
    return child;
  }

  const element = child as ReactElement<{ className?: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }>;
  const isFormControl =
    typeof element.type === 'string' &&
    (element.type === 'input' || element.type === 'select' || element.type === 'textarea');
  const className = [element.props.className, error ? 'is-invalid' : '', isFormControl ? 'eh-input' : '']
    .filter(Boolean)
    .join(' ');

  return cloneElement(element, {
    className: className || undefined,
    'aria-invalid': error ? true : element.props['aria-invalid'],
    'aria-describedby': describedBy ?? element.props['aria-describedby'],
  });
}

export function FormField({
  id,
  label,
  hint,
  error,
  required,
  children,
  className = '',
}: FormFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined;
  const enhancedChild = Children.map(children, (child) => enhanceControl(child, error, describedBy));

  return (
    <div className={`eh-form-field ${error ? 'eh-form-field--invalid' : ''} ${className}`.trim()}>
      <label className="form-label" htmlFor={id}>
        {label}
        {required ? (
          <span className="eh-required-mark" aria-hidden="true">
            *
          </span>
        ) : null}
        {required ? <span className="visually-hidden"> (required)</span> : null}
      </label>
      <div>{enhancedChild}</div>
      {hint ? (
        <div id={hintId} className="form-text">
          {hint}
        </div>
      ) : null}
      {error ? (
        <div id={errorId} className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      ) : null}
    </div>
  );
}
