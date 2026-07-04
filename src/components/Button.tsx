type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost";
};

export function Button({
  variant = "primary",
  className = "",
  ...props
}: ButtonProps) {
  const variants = {
    primary: "bg-amber-400 text-black hover:bg-amber-300",
    secondary: "border border-white/15 text-white hover:border-amber-400/50",
    ghost: "text-zinc-300 hover:text-white",
  };

  return (
    <button
      className={`rounded-full px-4 py-2 text-sm font-medium transition disabled:cursor-not-allowed disabled:opacity-50 ${variants[variant]} ${className}`}
      {...props}
    />
  );
}
