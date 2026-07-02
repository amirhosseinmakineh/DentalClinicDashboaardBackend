export const environment = {
  production: typeof window !== 'undefined' && !/localhost|127\.0\.0\.1/.test(window.location.hostname),
  apiBaseUrl: resolveApiBaseUrl()
};

function resolveApiBaseUrl(): string {
  if (typeof window === 'undefined') {
    return 'http://localhost:5182/api';
  }

  const { hostname, origin } = window.location;

  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return 'http://localhost:5182/api';
  }

  if (hostname.includes('runflare.run')) {
    return 'https://drmoghadam.runflare.run/api';
  }

  return `${origin}/api`;
}
