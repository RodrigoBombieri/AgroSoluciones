---
name: Industrial Logistics System
colors:
  surface: '#f8f9ff'
  surface-dim: '#cbdbf5'
  surface-bright: '#f8f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#eff4ff'
  surface-container: '#e5eeff'
  surface-container-high: '#dce9ff'
  surface-container-highest: '#d3e4fe'
  on-surface: '#0b1c30'
  on-surface-variant: '#42474e'
  inverse-surface: '#213145'
  inverse-on-surface: '#eaf1ff'
  outline: '#72777e'
  outline-variant: '#c2c7ce'
  surface-tint: '#3a6285'
  primary: '#002741'
  on-primary: '#ffffff'
  primary-container: '#0f3d5e'
  on-primary-container: '#81a8ce'
  inverse-primary: '#a3caf2'
  secondary: '#1b6d24'
  on-secondary: '#ffffff'
  secondary-container: '#a0f399'
  on-secondary-container: '#217128'
  tertiary: '#372000'
  on-tertiary: '#ffffff'
  tertiary-container: '#543300'
  on-tertiary-container: '#e49200'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#cee5ff'
  primary-fixed-dim: '#a3caf2'
  on-primary-fixed: '#001d33'
  on-primary-fixed-variant: '#204a6b'
  secondary-fixed: '#a3f69c'
  secondary-fixed-dim: '#88d982'
  on-secondary-fixed: '#002204'
  on-secondary-fixed-variant: '#005312'
  tertiary-fixed: '#ffddb8'
  tertiary-fixed-dim: '#ffb95f'
  on-tertiary-fixed: '#2a1700'
  on-tertiary-fixed-variant: '#653e00'
  background: '#f8f9ff'
  on-background: '#0b1c30'
  surface-variant: '#d3e4fe'
typography:
  display:
    fontFamily: Inter
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 44px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  headline-sm:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-lg:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
  mono-data:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  xs: 4px
  sm: 12px
  md: 16px
  lg: 24px
  xl: 32px
  grid-columns: '12'
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 32px
---

## Brand & Style

The design system is engineered for high-stakes industrial environments, specifically agro-logistics and weighing operations. The brand personality is **authoritative, robust, and precision-oriented**. It draws heavy inspiration from the efficiency of SAP Fiori and the layered, functional aesthetic of Microsoft Fluent Design.

The visual style follows a **Modern Corporate** approach. It prioritizes clarity over decoration, using generous whitespace to reduce cognitive load for operators who manage complex data streams. The interface conveys stability and trust through a structured grid, high-contrast functional elements, and a sophisticated color palette that feels at home in both a corporate boardroom and a grain elevator control room.

## Colors

The color strategy is divided into functional roles to ensure instant recognizability of status and intent.

- **Primary (Deep Navy Blue):** Used for global navigation, primary actions, and headers to establish authority and professional grounding.
- **Secondary/Success (Agricultural Green):** Reserved for "Cartas de Porte" approval, successful weighing operations, and "Go" states.
- **Alert/Warning (Industrial Orange):** Used for critical notifications, weight discrepancies, and pending documentation that requires immediate attention.
- **Surface & Background:** A cool Light Gray background separates the application shell from the pure white content containers, creating a layered effect that mimics physical documentation folders.

The design system supports a dark mode variant where the Navy Blue shifts to a deeper midnight tone and surfaces adopt a charcoal gray hierarchy to maintain accessibility in low-light industrial settings.

## Typography

This design system utilizes **Inter** as the primary typeface due to its exceptional legibility in data-heavy environments and its neutral, professional character. 

- **Scale:** A tight typographic scale is used to maximize information density without sacrificing readability.
- **Data Display:** For numerical values in weight scales and "Cartas de Porte" IDs, a monospaced font (JetBrains Mono) is used to ensure digit alignment in tables.
- **Hierarchy:** High-weight headers in Navy Blue clearly define section boundaries, while medium-weight labels ensure form fields are scannable.

## Layout & Spacing

The layout is built on a **12-column fluid grid** with a fixed 8px baseline rhythm. 

- **Desktop:** The sidebar is docked at 260px width. Content area uses a 24px gutter.
- **Containers:** White surfaces (cards) use 24px internal padding for standard data and 16px for condensed lists.
- **Density:** The system allows for a "Compact" mode for expert users, reducing the base unit from 8px to 4px in data tables to display more rows per screen.
- **Responsive Behavior:** On mobile, columns collapse to a single stack, and horizontal scrolling is enabled specifically for wide data tables with a persistent "Action" column.

## Elevation & Depth

This design system uses **Tonal Layering** combined with subtle **Fluent Shadows** to create a clear spatial hierarchy.

1.  **Level 0 (Background):** Light Gray (#F5F7FA). The foundation layer.
2.  **Level 1 (Cards/Tables):** Pure White (#FFFFFF) with a 1px border (#E2E8F0) and a very soft 4px blur shadow. This is the primary work surface.
3.  **Level 2 (Dropdowns/Modals):** Floating elements with a 12px blur shadow and 0.1 opacity, creating a distinct "lift" from the work surface.
4.  **Level 3 (Alert Overlays):** Used for urgent weight-bridge interventions, using a semi-transparent backdrop blur (8px) to focus the user on the task at hand.

Borders are used intentionally as the primary separator rather than heavy shadows to maintain the clean, "unrefined" industrial feel.

## Shapes

The shape language uses **Level 2 (Rounded)** settings to balance the coldness of industrial data with modern software sensibilities.

- **Standard Elements:** Buttons, Input fields, and KPI cards use a **8px (0.5rem)** corner radius.
- **Large Containers:** Main content panels and modals use **16px (1rem)** to soften the overall interface structure.
- **Interactive States:** On-hover, buttons maintain their shape but may receive a subtle inner-glow to indicate activity.
- **Status Badges:** Use a fully rounded "pill" shape (999px) to distinguish them from interactive buttons.

## Components

### KPI Cards
Feature a large monospaced value, a descriptive label, and a small sparkline (success green or warning orange) in the bottom quadrant. Icons are housed in a 40x40px rounded-lg container with 10% opacity of the primary color.

### Data Tables
- **Header:** Sticky with a subtle bottom border; text is uppercase 12px Medium weight.
- **Rows:** Zebra-striping using #F8FAFC on even rows.
- **Badges:** Success (Green), Warning (Orange), and Neutral (Gray) pills for "Cartas de Porte" status (Approved, Pending, Canceled).

### Sidebar
A high-contrast collapsible navigation. Icons are 24px, stroke-based. Active states use a 4px left-aligned primary blue indicator and a subtle background tint.

### Form Fields
Inputs use a white background with a 1px #CBD5E1 border. Focus states transition the border to Primary Navy Blue with a 2px outer halo. Validation errors appear directly below the field in 12px red text.

### Buttons
- **Primary:** Solid Navy Blue with white text.
- **Secondary:** Outline Navy Blue with 1px border.
- **Actionable Icons:** Minimalist ghost buttons that appear on row hover in data tables.