// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Teslalogger',
  tagline: 'Teslalogger Docs',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
   url: 'https://teslalogger.de',
   // Set the /<baseUrl>/ pathname under which your site is served
   baseUrl: '/docs/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'bassmaster187', // Usually your GitHub org/user name.
  projectName: 'TeslaLogger', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
       defaultLocale: 'de',
       locales: ['de', 'en'],
       localeConfigs: {
         de: {
           htmlLang: 'de-DE',
           label: 'Deutsch',
         },
         en: {
           htmlLang: 'en-US',
           label: 'English',
         },
       },
     },

   plugins: [
     [
       'docusaurus-lunr-search',
       {
         languages: ['de', 'en'],
         excludeSearchResultsPrefix: ['/en/docs/en/'],
       },
     ],
   ],
  
  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
               routeBasePath: '',
              path: '../docs',
              editUrl: ({locale, versionDocsDirPath, docPath}) => {
                      if (locale === 'de') {
                        return `https://github.com/bassmaster187/TeslaLogger/blob/master/docs/${docPath}`;
                      }
                      return `https://github.com/bassmaster187/TeslaLogger/blob/master/docs/${locale}/${docPath}`;
                    },
              sidebarPath: './sidebars.js',
              exclude: ['**/en/**'],
            },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/logo.jpg',
      navbar: {
        title: 'Teslalogger',
        logo: {
          alt: 'EMDS Logo',
          src: 'img/logo.jpg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Docs',
          },
          {
            href: 'https://github.com/bassmaster187/TeslaLogger',
            label: 'GitHub',
            position: 'right',
          },
          {
            type: 'localeDropdown',
            position: 'right',
          }
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Tutorial',
                to: '/docs',
              },
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'Github',
                href: 'https://github.com/bassmaster187/TeslaLogger',
              },
              {
                label: 'TFF Forum',
                href: 'https://tff-forum.de/t/teslalogger-mit-raspberry-pi-mysql-grafana-osm-teil-3/345688',
              },
              {
                label: 'TMC Tesla Motors Club',
                href: 'https://teslamotorsclub.com/tmc/threads/open-source-teslalogger-on-raspberry-docker-with-scanmytesla-integration.192363/',
              },
            ],
          },
          {
            title: 'More',
            items: [
              {
                label: 'Blog',
                href: 'https://www.impala64.de/blog/tesla/',
              },
              {
                label: 'GitHub',
                href: 'https://github.com/bassmaster187/TeslaLogger',
              },
            ],
          },
        ],
        
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
      },
    }),
};

export default config;
