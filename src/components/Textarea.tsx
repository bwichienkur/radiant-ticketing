type TextareaProps = React.TextareaHTMLAttributes<HTMLTextAreaElement> & {
  label: string;
};

export function Textarea({ label, className = "", ...props }: TextareaProps) {
  return (
    <label className="block space-y-1 text-sm">
      <span className="text-zinc-300">{label}</span>
      <textarea
        className={`w-full rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-white outline-none ring-amber-400/40 placeholder:text-zinc-500 focus:ring-2 ${className}`}
        {...props}
      />
    </label>
  );
}
