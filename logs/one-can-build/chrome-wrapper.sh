#!/bin/sh
exec /Users/sitpo/Library/Caches/ms-playwright/chromium_headless_shell-1234/chrome-headless-shell-mac-arm64/chrome-headless-shell \
  --no-sandbox \
  --single-process \
  --no-zygote \
  --disable-gpu \
  --disable-software-rasterizer \
  --disable-dev-shm-usage \
  --disable-breakpad \
  --disable-crash-reporter \
  --disable-features=Vulkan,UseChromeOSDirectVideoDecoder,OptimizationHints,MediaRouter \
  "$@"
