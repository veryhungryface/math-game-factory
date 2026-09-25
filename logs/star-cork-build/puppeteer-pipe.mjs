// Existing one-can restricted-browser setup. This changes transport/launch only.
import puppeteer from 'puppeteer';
export const executablePath = '/Users/sitpo/Library/Caches/ms-playwright/chromium_headless_shell-1234/chrome-headless-shell-mac-arm64/chrome-headless-shell';
export const survivalArgs = ['--no-sandbox', '--single-process', '--no-zygote', '--disable-gpu', '--disable-software-rasterizer', '--disable-dev-shm-usage', '--disable-breakpad', '--disable-crash-reporter', '--disable-features=Vulkan,UseChromeOSDirectVideoDecoder,OptimizationHints,MediaRouter'];
export default {
  ...puppeteer,
  launch(options = {}) {
    return puppeteer.launch({ ...options, headless: 'shell', pipe: true, executablePath, args: [...(options.args || []), ...survivalArgs] });
  },
};
