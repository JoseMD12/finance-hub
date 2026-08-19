#!/usr/bin/env node

/**
 * FinanceHub Automated Microservices API Test Suite Runner
 * Executes all health checks, domain validation, and Open Finance sync tests in a single command.
 */

const http = require('node:http');
const https = require('node:https');
const { URL } = require('node:url');

// Parse input configuration object from CLI argument or environment variables
function parseConfig() {
  const arg = process.argv[2];
  let config = {};

  if (arg) {
    try {
      config = JSON.parse(arg);
    } catch {
      console.error('⚠️  Failed to parse input argument as JSON. Falling back to defaults.');
    }
  }

  return {
    token: config.token || process.env.PLUGGY_ACCESS_TOKEN || '',
    userId: config.userId || process.env.TEST_USER_ID || 'josehenriquedotta61@gmail.com',
    urls: {
      pluggy: config.urls?.pluggy || process.env.PLUGGY_SERVICE_URL || 'http://localhost:5056',
      gateway: config.urls?.gateway || process.env.GATEWAY_SERVICE_URL || 'http://localhost:5050',
      aggregator: config.urls?.aggregator || process.env.AGGREGATOR_SERVICE_URL || 'http://localhost:5002',
    }
  };
}

function makeRequest(urlStr, options = {}) {
  return new Promise((resolve) => {
    const url = new URL(urlStr);
    const client = url.protocol === 'https:' ? https : http;
    const reqOptions = {
      hostname: url.hostname,
      port: url.port || (url.protocol === 'https:' ? 443 : 80),
      path: url.pathname + url.search,
      method: options.method || 'GET',
      headers: options.headers || {}
    };

    const startTime = Date.now();
    const req = client.request(reqOptions, (res) => {
      let body = '';
      res.on('data', (chunk) => { body += chunk; });
      res.on('end', () => {
        const durationMs = Date.now() - startTime;
        let parsed = null;
        try {
          parsed = JSON.parse(body);
        } catch {
          parsed = body;
        }

        resolve({
          statusCode: res.statusCode,
          headers: res.headers,
          body: parsed,
          rawBody: body,
          durationMs
        });
      });
    });

    req.on('error', (err) => {
      resolve({
        statusCode: 0,
        error: err.message,
        durationMs: Date.now() - startTime
      });
    });

    if (options.body) {
      req.write(typeof options.body === 'object' ? JSON.stringify(options.body) : options.body);
    }

    req.end();
  });
}

async function runTestSuite() {
  const config = parseConfig();
  console.log('\n======================================================');
  console.log('🚀 FinanceHub Autonomous API Test Suite Runner');
  console.log('======================================================');
  console.log(`📌 User ID: ${config.userId}`);
  console.log(`📌 Pluggy URL: ${config.urls.pluggy}`);
  console.log(`📌 Gateway URL: ${config.urls.gateway}`);
  console.log(`📌 Aggregator URL: ${config.urls.aggregator}`);
  console.log(`📌 Token Provided: ${config.token ? 'Yes (Length: ' + config.token.length + ')' : 'No'}`);
  console.log('------------------------------------------------------\n');

  const results = [];

  // Helper to record test results
  function record(name, endpoint, expectedStatus, res) {
    const passed = res.statusCode === expectedStatus;
    results.push({
      name,
      endpoint,
      expectedStatus,
      actualStatus: res.statusCode,
      passed,
      durationMs: res.durationMs,
      details: res.body
    });

    const statusIcon = passed ? '🟢 PASS' : '🔴 FAIL';
    console.log(`[${statusIcon}] ${name}`);
    console.log(`         URL: ${endpoint}`);
    console.log(`         Status: ${res.statusCode} (Expected: ${expectedStatus}) | Latency: ${res.durationMs}ms`);
    if (!passed) {
      console.log(`         Error Details:`, JSON.stringify(res.body || res.error, null, 2));
    }
    console.log('');
  }

  // 1. Health Checks
  console.log('--- 1. Health Checks ---');
  record('PluggyIntegration Health', `${config.urls.pluggy}/health`, 200, await makeRequest(`${config.urls.pluggy}/health`));
  record('ApiGateway Health', `${config.urls.gateway}/health`, 200, await makeRequest(`${config.urls.gateway}/health`));
  record('TransactionAggregator Health', `${config.urls.aggregator}/health`, 200, await makeRequest(`${config.urls.aggregator}/health`));

  // 2. Domain Exception (RFC 7807) Validation without token
  console.log('--- 2. Domain Validation (Missing Token) ---');
  record(
    'Missing Token RFC 7807 Check',
    `${config.urls.pluggy}/api/v1/pluggy/items`,
    400,
    await makeRequest(`${config.urls.pluggy}/api/v1/pluggy/items`)
  );

  // 3. Pluggy Integration Endpoints (With Token)
  if (config.token) {
    console.log('--- 3. Pluggy Open Finance Integration Endpoints ---');
    const authHeader = { 'X-Pluggy-Access-Token': config.token, 'Content-Type': 'application/json' };

    // 3.1 GET Items
    const itemsRes = await makeRequest(`${config.urls.pluggy}/api/v1/pluggy/items`, { headers: authHeader });
    record('GET /items (Connected Institutions)', `${config.urls.pluggy}/api/v1/pluggy/items`, 200, itemsRes);

    // 3.2 GET Accounts
    const accountsRes = await makeRequest(`${config.urls.pluggy}/api/v1/pluggy/accounts`, { headers: authHeader });
    record('GET /accounts (Bank & Credit Accounts)', `${config.urls.pluggy}/api/v1/pluggy/accounts`, 200, accountsRes);

    // 3.3 POST Batch Sync
    const syncRes = await makeRequest(`${config.urls.pluggy}/api/v1/pluggy/sync?userId=${encodeURIComponent(config.userId)}`, {
      method: 'POST',
      headers: authHeader
    });
    record('POST /sync (Full Portfolio Ingestion)', `${config.urls.pluggy}/api/v1/pluggy/sync`, 200, syncRes);

  } else {
    console.log('⚠️  Skipping token-authenticated tests (No token provided in config).');
  }

  // Summary Table
  const total = results.length;
  const passedCount = results.filter(r => r.passed).length;
  const failedCount = total - passedCount;

  console.log('\n======================================================');
  console.log('📊 Test Suite Execution Summary');
  console.log('======================================================');
  console.log(`Total Executed: ${total}`);
  console.log(`Passed: ${passedCount} 🟢`);
  console.log(`Failed: ${failedCount} ${failedCount > 0 ? '🔴' : '🟢'}`);
  console.log('======================================================\n');

  // Emit structured JSON output
  console.log('RESULT_JSON_START');
  console.log(JSON.stringify({
    timestamp: new Date().toISOString(),
    summary: { total, passed: passedCount, failed: failedCount },
    results
  }, null, 2));
  console.log('RESULT_JSON_END');

  if (failedCount > 0) {
    process.exit(1);
  }
}

try {
  await runTestSuite();
} catch (err) {
  console.error('Fatal error during test suite execution:', err);
  process.exit(1);
}
