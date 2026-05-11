/**
 * Returns a value-prefixed unique string so concurrent or repeated test runs don't
 * collide on the sample's unique indexes (Category.Name, Ingredient.Name, etc.).
 */
export function unique(prefix: string): string {
  const stamp = Date.now().toString(36);
  const noise = Math.random().toString(36).slice(2, 8);
  return `${prefix}-${stamp}-${noise}`;
}
