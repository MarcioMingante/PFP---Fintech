"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { navigation } from "@/config/navigation";

export function Navigation() {
  const pathname = usePathname();
  return (
    <nav aria-label="Navegação principal" className="navigation">
      {navigation.map((item) => (
        <Link key={item.href} href={item.href} aria-current={pathname === item.href ? "page" : undefined}>
          <span aria-hidden="true" className="nav-icon">{item.initial}</span>
          {item.label}
        </Link>
      ))}
    </nav>
  );
}
