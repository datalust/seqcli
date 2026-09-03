#!/usr/bin/env node
'use strict';

// Launcher for the platform-specific seqcli binary. The binary itself ships in one of the
// `@datalust/seqcli-<rid>` packages, installed as an optional dependency of `@datalust/seqcli`
// and selected by npm using the `os`/`cpu`/`libc` fields in each package.

const { spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const SCOPE = '@datalust';
const RELEASES_URL = 'https://github.com/datalust/seqcli/releases';

// `${process.platform}-${process.arch}` (with `-musl` inserted for musl-based Linux) -> .NET RID.
const RIDS = {
  'win32-x64': 'win-x64',
  'win32-arm64': 'win-arm64',
  'darwin-x64': 'osx-x64',
  'darwin-arm64': 'osx-arm64',
  'linux-x64': 'linux-x64',
  'linux-arm64': 'linux-arm64',
  'linux-musl-x64': 'linux-musl-x64',
  'linux-musl-arm64': 'linux-musl-arm64',
};

function isMusl() {
  try {
    return !process.report.getReport().header.glibcVersionRuntime;
  } catch {
    return false;
  }
}

// Candidate platform packages in order of preference. On Linux the detected libc variant is tried
// first, then the other one, so that package managers that ignore the `libc` field (and therefore
// install both) still run the right binary.
function candidatePackages() {
  const { platform, arch } = process;
  const keys = [];
  if (platform === 'linux') {
    const musl = isMusl();
    keys.push(musl ? `linux-musl-${arch}` : `linux-${arch}`);
    keys.push(musl ? `linux-${arch}` : `linux-musl-${arch}`);
  } else {
    keys.push(`${platform}-${arch}`);
  }
  return keys.filter((k) => RIDS[k]).map((k) => `${SCOPE}/seqcli-${RIDS[k]}`);
}

function locateBinary() {
  const launcherVersion = require('../package.json').version;
  const candidates = candidatePackages();
  const problems = [];

  for (const name of candidates) {
    let packageJsonPath;
    try {
      packageJsonPath = require.resolve(`${name}/package.json`);
    } catch {
      problems.push(`${name} is not installed`);
      continue;
    }

    const installedVersion = require(packageJsonPath).version;
    if (installedVersion !== launcherVersion) {
      problems.push(`${name}@${installedVersion} does not match ${SCOPE}/seqcli@${launcherVersion}`);
      continue;
    }

    const exe = path.join(path.dirname(packageJsonPath), process.platform === 'win32' ? 'seqcli.exe' : 'seqcli');
    if (!fs.existsSync(exe)) {
      problems.push(`${name} is installed but ${exe} is missing`);
      continue;
    }

    return exe;
  }

  const lines = [];
  if (candidates.length === 0) {
    lines.push(`seqcli: ${process.platform}-${process.arch} is not supported by the npm package.`);
  } else {
    lines.push('seqcli: could not find the platform-specific seqcli package.');
    for (const p of problems) lines.push(`  - ${p}`);
    lines.push('');
    lines.push(`Reinstall with: npm install -g ${SCOPE}/seqcli@${launcherVersion}`);
    lines.push('(optional dependencies must not be omitted; check for --omit=optional / --no-optional)');
    lines.push(`or install the platform package directly: npm install -g ${candidates[0]}@${launcherVersion}`);
  }
  lines.push('');
  lines.push(`Supported platforms: ${Object.values(RIDS).join(', ')}.`);
  lines.push(`Other downloads: ${RELEASES_URL}`);
  console.error(lines.join('\n'));
  process.exit(1);
}

function ensureExecutable(exe) {
  if (process.platform === 'win32') return;
  try {
    fs.accessSync(exe, fs.constants.X_OK);
  } catch {
    try {
      fs.chmodSync(exe, 0o755);
    } catch {
      // Reported by spawn() as EACCES below.
    }
  }
}

function run() {
  const exe = locateBinary();
  ensureExecutable(exe);

  const child = spawn(exe, process.argv.slice(2), { stdio: 'inherit', windowsHide: true });

  // Ctrl+C is delivered by the terminal to the whole foreground process group (or console on
  // Windows), so the child already receives it. Ignore it here so this process outlives the
  // child and can report the child's exit status.
  process.on('SIGINT', () => {});

  // Signals from supervisors (kill, systemd, CI cancellation) target this process only; forward them.
  for (const signal of ['SIGTERM', 'SIGHUP']) {
    process.on(signal, () => child.kill(signal));
  }

  child.on('error', (err) => {
    console.error(`seqcli: failed to start ${exe}: ${err.message}`);
    process.exit(1);
  });

  child.on('exit', (code, signal) => {
    if (signal) {
      process.removeAllListeners(signal);
      try {
        process.kill(process.pid, signal);
      } catch {
        // Fall through to a conventional exit code.
      }
      process.exit(128 + (os.constants.signals[signal] || 0));
    }
    process.exit(code === null ? 1 : code);
  });
}

run();
