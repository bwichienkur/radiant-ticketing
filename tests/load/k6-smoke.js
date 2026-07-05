import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:5075';
const vus = Number(__ENV.VUS || 10);
const duration = __ENV.DURATION || '30s';

export const options = {
  vus,
  duration,
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
  },
};

export default function () {
  const health = http.get(`${baseUrl}/health`);
  check(health, { 'health ok': (r) => r.status === 200 });

  const requests = http.get(`${baseUrl}/api/v1/EnhancementRequests`, {
    headers: { Authorization: `Bearer ${__ENV.API_TOKEN || ''}` },
  });
  check(requests, {
    'v1 requests reachable': (r) => r.status === 200 || r.status === 401,
  });

  sleep(1);
}
