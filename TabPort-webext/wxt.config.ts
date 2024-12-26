import { defineConfig } from 'wxt';

// See https://wxt.dev/api/config.html
export default defineConfig({
  extensionApi: 'chrome',
  modules: ['@wxt-dev/module-react', '@wxt-dev/auto-icons'],
  srcDir: 'src',
  manifest: {
    permissions: ['storage', 'tabs', 'scripting', 'activeTab'],
    host_permissions: ['<all_urls>'],
    description: "A simple WebSocket client that connects to a PowerToys Run plugin, allowing you to switch browser tabs directly from the PowerToys Run interface. By establishing a connection between your browser and PowerToys Run, you can effortlessly navigate between your open tabs using keyboard shortcuts or PowerToys Run's search bar. Streamline your workflow and boost your productivity with this convenient integration.",
  },
});