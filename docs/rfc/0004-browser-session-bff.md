# RFC-0004: Browser session hardening with a backend for frontend

- **Status:** Draft
- **Authors:** Repository maintainer
- **Created:** 2026-07-26

## Summary

Replace long-lived bearer-token storage in Blazor WebAssembly local storage with
a same-origin backend-for-frontend (BFF) session that uses Secure, HttpOnly,
SameSite cookies and keeps refresh credentials out of browser script storage.

## Motivation

The current client is simple and functional, but a successful same-origin script
injection can read bearer tokens from local storage. The API's hashed token
storage protects a database leak; it does not remove this browser threat.

## Proposed direction

- Add a small same-origin BFF that owns login, refresh, logout, and anti-forgery.
- Store server-side session state or a protected refresh credential outside
  browser-accessible storage.
- Proxy API and SignalR traffic through the BFF.
- Use short access-token lifetimes internally and rotate refresh credentials.
- Define CSRF, SameSite, logout-all-sessions, and multi-device behavior.

## Required acceptance evidence

- browser end-to-end login, refresh, logout, expiry, and reconnect tests;
- CSRF and cross-origin negative tests;
- XSS-impact review showing refresh credentials are not script-readable;
- SignalR authentication and reconnect tests;
- migration plan for existing local-storage sessions;
- updated threat model, C4 deployment view, and operations runbook.

## Non-goals

This RFC does not claim that a BFF eliminates XSS or replaces content-security
policy, dependency hygiene, output encoding, TLS, or normal browser hardening.
