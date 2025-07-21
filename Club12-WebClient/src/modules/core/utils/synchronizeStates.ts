export function upsertListById<T extends { id: string }>(
  list: T[] | null | undefined,
  item: T
): T[] {
  const safeList = list ?? [];

  const index = safeList.findIndex(x => x.id === item.id);
  if (index !== -1) {
    const newList = [...safeList];
    newList[index] = item;
    return newList;
  } else {
    return [...safeList, item];
  }
}
