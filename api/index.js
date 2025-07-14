// Vercel Serverless Function wrapper for SQL Analyzer API
// This allows running ASP.NET Core API on Vercel using Edge Functions

const { createServer } = require('@vercel/node');
const { exec } = require('child_process');
const path = require('path');

// Environment configuration
const PORT = process.env.PORT || 3000;
const API_PATH = path.join(__dirname, '../src/SqlAnalyzer.Api');

// Start the .NET API process
let apiProcess;

async function startApi() {
  return new Promise((resolve, reject) => {
    apiProcess = exec(
      `dotnet run --urls http://localhost:${PORT}`,
      { cwd: API_PATH },
      (error, stdout, stderr) => {
        if (error) {
          console.error(`API Error: ${error}`);
          reject(error);
        }
      }
    );

    // Wait for API to start
    setTimeout(() => {
      resolve();
    }, 5000);
  });
}

// Proxy requests to the .NET API
module.exports = async (req, res) => {
  try {
    // Start API if not running
    if (!apiProcess) {
      await startApi();
    }

    // Proxy the request
    const apiUrl = `http://localhost:${PORT}${req.url}`;
    const response = await fetch(apiUrl, {
      method: req.method,
      headers: req.headers,
      body: req.method !== 'GET' ? JSON.stringify(req.body) : undefined,
    });

    // Forward response
    const data = await response.text();
    res.status(response.status);
    
    // Copy headers
    response.headers.forEach((value, key) => {
      res.setHeader(key, value);
    });
    
    res.send(data);
  } catch (error) {
    console.error('Proxy error:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
};

// Cleanup on exit
process.on('exit', () => {
  if (apiProcess) {
    apiProcess.kill();
  }
});