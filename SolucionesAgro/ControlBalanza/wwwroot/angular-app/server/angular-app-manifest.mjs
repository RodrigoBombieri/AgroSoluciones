
export default {
  bootstrap: () => import('./main.server.mjs').then(m => m.default),
  inlineCriticalCss: true,
  baseHref: '/',
  locale: undefined,
  routes: [
  {
    "renderMode": 2,
    "route": "/"
  }
],
  entryPointToBrowserMapping: undefined,
  assets: {
    'index.csr.html': {size: 416, hash: '43f909c0faee2399a8c520e03cd2f4bd97d209bc867d1805647d9815e877aeba', text: () => import('./assets-chunks/index_csr_html.mjs').then(m => m.default)},
    'index.server.html': {size: 929, hash: 'e5d2ffeb9f7718968bbb187ca07e4993442563a8cf0b296226ee410911519f47', text: () => import('./assets-chunks/index_server_html.mjs').then(m => m.default)},
    'index.html': {size: 21458, hash: 'f769ff2988dfcfcb1d3934f32ae7e3d5e9bf7f7a1d3365570dfa55ae329108d0', text: () => import('./assets-chunks/index_html.mjs').then(m => m.default)},
    'styles.css': {size: 0, hash: 'menYUTfbRu8', text: () => import('./assets-chunks/styles_css.mjs').then(m => m.default)}
  },
};
