import React, { useState } from "react";
import { useBlogPost } from "../../modules/blogPost/hook/blogPost.hook";
import { AddBlogPostRequest } from "../../modules/blogPost/type/blogPost";

const AddBlogPostForm: React.FC = () => {
  const { addBlogPost } = useBlogPost();
  const [formData, setFormData] = useState<AddBlogPostRequest>({
    author: "",
    title: "",
    markdownText: "",
  });

  const handleInputChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    //formData.photoFile = new File();
    addBlogPost(formData);
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="author">Author:</label>
        <input
          type="text"
          id="author"
          name="author"
          value={formData.author}
          onChange={handleInputChange}
          required
        />
      </div>

      <div>
        <label htmlFor="title">Title:</label>
        <input
          type="text"
          id="title"
          name="title"
          value={formData.title}
          onChange={handleInputChange}
          required
        />
      </div>

      <div>
        <label htmlFor="markdownText">Markdown Text:</label>
        <textarea
          id="markdownText"
          name="markdownText"
          value={formData.markdownText}
          onChange={handleInputChange}
          required
        ></textarea>
      </div>

      <button type="submit">Submit</button>
    </form>
  );
};

export default AddBlogPostForm;
