import { expect, Locator, Page } from '@playwright/test';

/**
 * Shared UI interaction helpers built on role-based locators.
 * Selector conventions are documented in the project README.
 */

/** Navigates via the sidebar and waits for the route to change. */
export async function navigateTo(page: Page, itemText: string, expectedPath: RegExp): Promise<void> {
  await page.getByRole('button', { name: itemText, exact: true }).click();
  await expect(page).toHaveURL(expectedPath);
}

/** Logs out through the app bar button and waits for the login page. */
export async function logout(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Logout' }).click();
  await expect(page).toHaveURL(/\/login/);
}

/** The MUI DataGrid on the page (single-grid pages). */
export function grid(page: Page): Locator {
  return page.getByRole('grid');
}

/** A DataGrid row identified by some of its cell content. */
export function gridRow(page: Page, content: string): Locator {
  return grid(page).getByRole('row').filter({ hasText: content });
}

/** Waits until the DataGrid has finished loading and shows at least one data row. */
export async function waitForGridRows(page: Page): Promise<void> {
  await expect(grid(page).getByRole('row').nth(1)).toBeVisible();
}

/** Clicks a row action (Edit/Delete buttons rendered in the actions column). */
export async function clickRowAction(page: Page, rowContent: string, action: 'Edit' | 'Delete'): Promise<void> {
  await gridRow(page, rowContent).getByRole('menuitem', { name: action })
    .or(gridRow(page, rowContent).getByRole('button', { name: action }))
    .first()
    .click();
}

/** The currently open MUI dialog. */
export function dialog(page: Page): Locator {
  return page.getByRole('dialog');
}

/** Fills a labelled text field inside the open dialog (MUI renders required labels as "X *"). */
export async function fillDialogField(page: Page, label: string, value: string): Promise<void> {
  // exact: the accessible name is the bare label, and substrings collide (Name/Surname)
  await dialog(page).getByRole('textbox', { name: label, exact: true }).fill(value);
}

/** Clicks a button inside the open dialog (e.g. Save, Cancel, Delete, Create). */
export async function clickDialogButton(page: Page, name: string): Promise<void> {
  await dialog(page).getByRole('button', { name }).click();
}

/** Asserts that a success/error snackbar with the given text appears. */
export async function expectToast(page: Page, text: string | RegExp): Promise<void> {
  await expect(page.getByRole('alert').filter({ hasText: text })).toBeVisible();
}

/** Signs in through the real login form. */
export async function loginViaForm(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.getByRole('textbox', { name: 'Email' }).fill(email);
  await page.getByRole('textbox', { name: 'Password' }).fill(password);
  await page.getByRole('button', { name: 'Sign In' }).click();
}
