type InputProps = React.InputHTMLAttributes<HTMLInputElement> & {
  label: string;
};

export function Input({ label, className = "", ...props }: InputProps) {
  return (
    <label className="block space-y-1 text-sm">
      <span className="text-zinc-300">{label}</span>
      <input
        className={`w-full rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-white outline-none ring-amber-400/40 placeholder:text-zinc-500 focus:ring-2 ${className}`}
        {...props}
      />
    </label>
  );
}
