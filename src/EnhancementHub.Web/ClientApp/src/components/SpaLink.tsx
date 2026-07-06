import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { isSpaPath } from '../spaRoutes';

interface SpaLinkProps extends AnchorHTMLAttributes<HTMLAnchorElement> {
  href: string;
  children: ReactNode;
}

export function SpaLink({ href, children, ...rest }: SpaLinkProps) {
  if (!href || href.startsWith('http') || href.startsWith('mailto:') || href.startsWith('#')) {
    return (
      <a href={href} {...rest}>
        {children}
      </a>
    );
  }

  const url = new URL(href, window.location.origin);
  const internal = url.origin === window.location.origin;
  const spa = internal && isSpaPath(url.pathname);

  if (spa) {
    const to = `${url.pathname}${url.search}${url.hash}`;
    return (
      <Link to={to} {...rest}>
        {children}
      </Link>
    );
  }

  return (
    <a href={href} {...rest}>
      {children}
    </a>
  );
}
