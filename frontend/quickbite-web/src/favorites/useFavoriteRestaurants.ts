import { useCallback, useEffect, useMemo, useState } from "react";

const favoritesStorageKey = "quickbite.favoriteRestaurants";

export function useFavoriteRestaurants() {
  const [favoriteRestaurantIds, setFavoriteRestaurantIds] = useState<string[]>(() => loadFavoriteRestaurantIds());

  useEffect(() => {
    localStorage.setItem(favoritesStorageKey, JSON.stringify(favoriteRestaurantIds));
  }, [favoriteRestaurantIds]);

  const favoriteRestaurantIdSet = useMemo(() => new Set(favoriteRestaurantIds), [favoriteRestaurantIds]);
  const isFavorite = useCallback(
    (restaurantId: string) => favoriteRestaurantIdSet.has(restaurantId),
    [favoriteRestaurantIdSet]
  );
  const toggleFavorite = useCallback((restaurantId: string) => {
    setFavoriteRestaurantIds((currentIds) =>
      currentIds.includes(restaurantId)
        ? currentIds.filter((currentId) => currentId !== restaurantId)
        : [...currentIds, restaurantId]);
  }, []);

  return {
    favoriteRestaurantIds,
    isFavorite,
    toggleFavorite
  };
}

function loadFavoriteRestaurantIds() {
  const rawFavorites = localStorage.getItem(favoritesStorageKey);

  if (!rawFavorites) {
    return [];
  }

  try {
    const parsedFavorites = JSON.parse(rawFavorites);
    return Array.isArray(parsedFavorites)
      ? parsedFavorites.filter((value): value is string => typeof value === "string")
      : [];
  } catch {
    return [];
  }
}
