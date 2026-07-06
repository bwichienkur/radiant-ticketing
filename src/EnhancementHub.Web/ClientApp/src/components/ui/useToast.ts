interface ToastPayload {
  title?: string;
  message?: string;
  variant?: 'info' | 'success' | 'warning' | 'danger';
}

type EhUxWindow = Window & {
  EhUx?: {
    showToast?: (payload: ToastPayload) => void;
  };
};

export function useToast() {
  function show(variant: ToastPayload['variant'], title: string, message = '') {
    const ehUx = (window as EhUxWindow).EhUx;
    ehUx?.showToast?.({ title, message, variant });
  }

  return {
    info: (title: string, message?: string) => show('info', title, message),
    success: (title: string, message?: string) => show('success', title, message),
    warning: (title: string, message?: string) => show('warning', title, message),
    danger: (title: string, message?: string) => show('danger', title, message),
  };
}
