# Launcher Kit website
This site uses Jekyll, which is built into GitHub Pages. It converts markdown files into webpages.

## Overview of files
- `_config.yml` handles Jekyll configuration. It also defines variables which may be reused across pages.
- `CNAME` sets the website URL. This file should not be modified.
- `_layouts` contains the HTML templates used to generate the webpages. The contents of individual pages are automatically inserted into these templates to produce the website.
- `style` contains CSS, images, and fonts, which define the visual style of the website.
- `index.html` is the website homepage, containing the download link, and basic information and instructions.
- `support.md` is the "Info & Support" page, containing links to "General Info" and "Common Errors", as well as instructions for getting support from others.
- `_info` contains the pages that will appear under the "General Info" heading on the support pages.
- `_common-errors` contains the pages that will appear under the "Common Errors" heading on the support pages.
- `developers.md` is the "For Mod Developers" page, with basic info about making mods.

## How to...
### Add support pages
Create a markdown (.md) file in either `_info` or `_common-errors`. If you are copying a guide from Discord, it can likely be used as-is, as the markdown syntax is mostly compatible.

The file name will be used as the page URL, so choose something short and simple, yet unique and descriptive. It should use `snake-case`, i.e. no spaces or capital letters. The file name won't show on the page itself, only in the address bar. File names must be unique across both the `_info` and `_common-errors` folders.

Choose a title that is not overly-long, as it will be shown in the navigation list. Add it to the very top of the file, using this format:
```
---
title: An Appropriate Title
---
```

If you need more advanced formatting, you can optionally create an HTML (.html) file instead of markdown.

After pushing, the page will automatically be detected and added to the navigation list.

### Change URLs that are reused across pages
Some URLs used in links, for example, the links to Discord servers or specific downloads, are defined in the `_config.yml` file. Updating the URL in this file will change it across all pages where it is used.

Additional URLs or values may be added to the config file. They can be used in a page with `{{ page.variable_name }}`.

### Add top-level pages
If it becomes necessary to add a top-level page to the top navigation bar, create a top-level (i.e. not in a folder) file using markdown or HTML, and give it a title as described above. Update `_layouts/default.html` as described in that file.

### Add new categories for support pages
If "General Info" and "Common Errors" are not enough, additional headings can be added. Add them to `_config.yml` matching the existing format. Create a new top-level folder with the same name used, prefixed with `_`. Update `_layouts/support-page.html` as described in that file.