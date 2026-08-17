export function handleFields(event: React.FormEvent<HTMLFormElement>) {
  event.preventDefault();
  const fields = Object.fromEntries(new window.FormData(event.currentTarget));
  return fields;
}
